using NUnit.Framework;
using UnityEngine;
using ArcheryGame.UI;

namespace ArcheryGame.Tests
{
    /// <summary>
    /// Edit Mode tests for the head-locked score HUD.
    /// </summary>
    [TestFixture]
    public class ScoreHudTests
    {
        private GameObject _managerObject;
        private GameObject _headObject;
        private GameManager _gameManager;
        private ScoreHud _hud;

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("TestGameManager");
            _gameManager = _managerObject.AddComponent<GameManager>();
            _hud = _managerObject.AddComponent<ScoreHud>();
            _headObject = new GameObject("TestHead");
        }

        [TearDown]
        public void TearDown()
        {
            if (_headObject != null) Object.DestroyImmediate(_headObject);
            if (_managerObject != null) Object.DestroyImmediate(_managerObject);
        }

        [Test]
        public void FormatScore_ShowsLabelAndValue()
        {
            Assert.AreEqual("Pontos: 0", ScoreHud.FormatScore(0));
            Assert.AreEqual("Pontos: 48", ScoreHud.FormatScore(48));
        }

        [Test]
        public void Bind_ShowsCurrentScore()
        {
            _gameManager.AddScore(8);

            _hud.Bind(_gameManager);

            Assert.AreEqual("Pontos: 8", _hud.CurrentText);
        }

        [Test]
        public void ScoreChanges_UpdateHudText()
        {
            _hud.Bind(_gameManager);

            _gameManager.AddScore(16);
            _gameManager.AddScore(4);

            Assert.AreEqual("Pontos: 20", _hud.CurrentText);
        }

        [Test]
        public void ResetTargets_ResetsHudToZero()
        {
            _hud.Bind(_gameManager);
            _gameManager.AddScore(16);

            _gameManager.ResetTargets();

            Assert.AreEqual("Pontos: 0", _hud.CurrentText);
        }

        [Test]
        public void AttachTo_PlacesTextTopRightInFrontOfHead()
        {
            _hud.AttachTo(_headObject.transform);

            Assert.AreSame(_headObject.transform, _hud.AttachedHead);
            Vector3 local = _hud.LabelTransform.localPosition;
            Assert.Greater(local.x, 0f, "HUD should be to the right of the view center.");
            Assert.Greater(local.y, 0f, "HUD should be above the view center.");
            Assert.Greater(local.z, 0f, "HUD should be in front of the camera.");
        }
    }
}
