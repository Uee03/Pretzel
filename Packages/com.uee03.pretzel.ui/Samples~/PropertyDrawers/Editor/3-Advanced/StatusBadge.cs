using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// A small reusable status pill. It is a real control because several drawers need it and UXML or UI Builder can use it:
    /// <c>&lt;Pretzel.Samples.UI.PropertyDrawers.Editors.StatusBadge status="Warning" /&gt;</c>.
    /// </summary>
    [UxmlElement]
    public partial class StatusBadge : Label
    {
        public const string UssClassName = "pretzel-status-badge";

        private BadgeStatus _status;

        public StatusBadge()
        {
            AddToClassList(UssClassName);
            Apply();
        }

        [UxmlAttribute("status")]
        public BadgeStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                Apply();
            }
        }

        private void Apply()
        {
            EnableInClassList(UssClassName + "--ok", _status == BadgeStatus.Ok);
            EnableInClassList(UssClassName + "--warning", _status == BadgeStatus.Warning);
            EnableInClassList(UssClassName + "--error", _status == BadgeStatus.Error);
            text = _status == BadgeStatus.Ok ? "OK" : _status.ToString();
        }
    }
}
