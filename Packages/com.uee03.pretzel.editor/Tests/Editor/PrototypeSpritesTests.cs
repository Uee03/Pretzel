using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Pretzel.Editor.Prototyping;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Editor.Tests
{
    public sealed class PrototypeSpritesTests
    {
        private const string TestFolder = "Assets/PretzelPrototypeSpritesTest";

        [TearDown]
        public void DeleteFolder()
        {
            AssetDatabase.DeleteAsset(TestFolder);
        }

        [TestCase(PrototypeShape.Square)]
        [TestCase(PrototypeShape.RoundedSquare)]
        [TestCase(PrototypeShape.Circle)]
        [TestCase(PrototypeShape.Triangle)]
        [TestCase(PrototypeShape.Diamond)]
        [TestCase(PrototypeShape.Hexagon)]
        public void EveryShapeContainsItsCentre(PrototypeShape shape)
        {
            Assert.That(PrototypeSprites.Contains(shape, 0f, -0.2f), Is.True);
        }

        [Test]
        public void ShapesLeaveTheirCornersOutExceptTheSquare()
        {
            Assert.That(PrototypeSprites.Contains(PrototypeShape.Square, 0.95f, 0.95f), Is.True);
            Assert.That(PrototypeSprites.Contains(PrototypeShape.RoundedSquare, 0.98f, 0.98f), Is.False);
            Assert.That(PrototypeSprites.Contains(PrototypeShape.Circle, 0.8f, 0.8f), Is.False);
            Assert.That(PrototypeSprites.Contains(PrototypeShape.Diamond, 0.6f, 0.6f), Is.False);
            Assert.That(PrototypeSprites.Contains(PrototypeShape.Hexagon, 0.9f, 0.7f), Is.False);
        }

        [Test]
        public void TheTriangleNarrowsTowardsItsTip()
        {
            Assert.That(PrototypeSprites.Contains(PrototypeShape.Triangle, 0.9f, -0.95f), Is.True);
            Assert.That(PrototypeSprites.Contains(PrototypeShape.Triangle, 0.9f, 0.5f), Is.False);
            Assert.That(PrototypeSprites.Contains(PrototypeShape.Triangle, 0f, 0.95f), Is.True);
        }

        [Test]
        public void CoverageIsPartialOnTheEdgeOfACircle()
        {
            float centre = PrototypeSprites.Coverage(PrototypeShape.Circle, PrototypeSprites.Size / 2, PrototypeSprites.Size / 2);
            float corner = PrototypeSprites.Coverage(PrototypeShape.Circle, 0, 0);
            float edge = PrototypeSprites.Coverage(PrototypeShape.Circle, 54, 54);

            Assert.That(centre, Is.EqualTo(1f));
            Assert.That(corner, Is.EqualTo(0f));
            Assert.That(edge, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void CreateWritesImportedSpritesAndNeverOverwrites()
        {
            IReadOnlyList<string> created = PrototypeSprites.Create(TestFolder);

            Assert.That(created.Count, Is.EqualTo(6));
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(TestFolder + "/Circle.png");
            Assert.That(circle, Is.Not.Null);
            Assert.That(circle.pixelsPerUnit, Is.EqualTo(PrototypeSprites.Size));
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(TestFolder + "/RoundedSquare.png").border, Is.EqualTo(new Vector4(16f, 16f, 16f, 16f)));

            File.WriteAllText(TestFolder + "/Circle.png", "mine");
            Assert.That(PrototypeSprites.Create(TestFolder), Is.Empty);
            Assert.That(File.ReadAllText(TestFolder + "/Circle.png"), Is.EqualTo("mine"));
        }
    }
}
