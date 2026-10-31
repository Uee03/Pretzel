using System;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>
    /// A combined window: UXML layout, stat cards built from an enum, a ProgressBar driven by a scheduled scan and buttons
    /// that select, ping and reveal real assets.
    /// </summary>
    public sealed class DashboardWindow : EditorWindow
    {
        private const int StepBudget = 250;
        private const long StepIntervalMilliseconds = 16;

        private readonly ProjectScanner _scanner = new();
        private Label[] _valueLabels;
        private int[] _shownCounts;
        private ProgressBar _progress;
        private Label _status;
        private Button _scanButton;
        private Button _cancelButton;
        private Button _largestButton;
        private IVisualElementScheduledItem _scanTask;
        private int _shownPercent = -1;

        [MenuItem("Pretzel/Samples/Editor Windows/Dashboard")]
        private static void Open()
        {
            DashboardWindow window = GetWindow<DashboardWindow>();
            window.titleContent = new GUIContent("Dashboard");
            window.minSize = new Vector2(380f, 440f);
        }

        private void OnDisable()
        {
            _scanner.Cancel();
            _scanTask?.Pause();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("pretzel-sample");
            EditorUI.ApplySkinClass(root);
            SampleAssets.AddStyleSheets(root, "SampleWindows.uss", "DashboardWindow.uss");
            if (!SampleAssets.CloneLayout(root, "DashboardWindow.uxml"))
            {
                return;
            }

            BuildStatCards(root.Q<VisualElement>("dashboard__stats"));

            _progress = root.Q<ProgressBar>("dashboard__progress");
            _status = root.Q<Label>("dashboard__status");
            _scanButton = root.Q<Button>("dashboard__scan");
            _cancelButton = root.Q<Button>("dashboard__cancel");
            _largestButton = root.Q<Button>("dashboard__largest");

            _scanButton.clicked += StartScan;
            _cancelButton.clicked += CancelScan;
            _largestButton.clicked += SelectLargest;
            root.Q<Button>("dashboard__refresh").clicked += RefreshAssets;
            root.Q<Button>("dashboard__reveal").clicked += RevealAssets;
            root.Q<Button>("dashboard__settings").clicked += OpenPlayerSettings;

            // Created once and paused; Resume and Pause control the scan.
            _scanTask = root.schedule.Execute(ScanStep).Every(StepIntervalMilliseconds);
            _scanTask.Pause();

            SetScanning(false);
            _status.text = "Press Scan Project to count the assets in Assets/.";
        }

        private void BuildStatCards(VisualElement container)
        {
            string[] names = Enum.GetNames(typeof(AssetCategory));
            _valueLabels = new Label[names.Length];
            _shownCounts = new int[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                VisualElement card = new();
                card.AddToClassList("pretzel-dashboard__card");

                Label value = new("0");
                value.AddToClassList("pretzel-dashboard__card-value");
                card.Add(value);

                Label caption = new(names[i]);
                caption.AddToClassList("pretzel-dashboard__card-caption");
                card.Add(caption);

                _valueLabels[i] = value;
                container.Add(card);
            }
        }

        private void StartScan()
        {
            _scanner.Begin();
            _shownPercent = -1;
            SetScanning(true);
            _scanTask.Resume();
        }

        private void CancelScan()
        {
            _scanner.Cancel();
            _scanTask.Pause();
            SetScanning(false);
            _progress.title = "Cancelled";
            _status.text = "Scan cancelled.";
        }

        private void ScanStep()
        {
            bool done = _scanner.Step(StepBudget);
            UpdateProgress();
            UpdateCards();

            if (!done)
            {
                return;
            }

            _scanTask.Pause();
            SetScanning(false);
            _progress.title = "Done";
            _status.text = _scanner.LargestPath != null
                ? $"Scanned {_scanner.Total} paths. Largest asset: {_scanner.LargestPath}"
                : "Scan finished. No assets were found.";
        }

        private void UpdateProgress()
        {
            float fraction = _scanner.Total == 0 ? 1f : (float)_scanner.Processed / _scanner.Total;
            _progress.value = fraction * 100f;

            // Setting the title allocates, so only do it when the visible percentage changes.
            int percent = (int)(fraction * 100f);
            if (percent != _shownPercent)
            {
                _shownPercent = percent;
                _progress.title = percent + "%";
            }
        }

        private void UpdateCards()
        {
            for (int i = 0; i < _valueLabels.Length; i++)
            {
                int count = _scanner.Counts[i];
                if (count != _shownCounts[i])
                {
                    _shownCounts[i] = count;
                    _valueLabels[i].text = count.ToString();
                }
            }
        }

        private void SetScanning(bool scanning)
        {
            _scanButton.SetEnabled(!scanning);
            _cancelButton.SetEnabled(scanning);
            _largestButton.SetEnabled(!scanning && _scanner.LargestPath != null);
        }

        private void SelectLargest()
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(_scanner.LargestPath);
            if (asset)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void RefreshAssets()
        {
            AssetDatabase.Refresh();
            _status.text = "Asset database refreshed.";
        }

        private static void RevealAssets()
        {
            EditorUtility.RevealInFinder(Application.dataPath);
        }

        private static void OpenPlayerSettings()
        {
            SettingsService.OpenProjectSettings("Project/Player");
        }
    }
}
