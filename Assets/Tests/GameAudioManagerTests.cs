using NUnit.Framework;
using UnityEngine;
using ArcheryGame.Audio;

namespace ArcheryGame.Tests
{
    [TestFixture]
    public class GameAudioManagerTests
    {
        private GameObject _audioManagerObject;
        private GameAudioManager _audioManager;

        [SetUp]
        public void SetUp()
        {
            _audioManagerObject = new GameObject("TestGameAudioManager");
            _audioManager = _audioManagerObject.AddComponent<GameAudioManager>();
            _audioManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_audioManagerObject != null)
            {
                Object.DestroyImmediate(_audioManagerObject);
            }
        }

        [Test]
        public void Initialize_LoadsAllAudioClipsFromResources()
        {
            Assert.IsNotNull(_audioManager.BowDrawClip, "bow_draw clip should be loaded from Resources.");
            Assert.IsNotNull(_audioManager.BowReleaseClip, "bow_release clip should be loaded from Resources.");
            Assert.IsNotNull(_audioManager.ArrowHitTargetClip, "arrow_hit_target clip should be loaded from Resources.");
            Assert.IsNotNull(_audioManager.UiClickClip, "ui_click clip should be loaded from Resources.");
            Assert.IsNotNull(_audioManager.TargetActionOrMissClip, "target_action_or_miss clip should be loaded from Resources.");
        }

        [Test]
        public void AudioActions_ExecuteWithoutThrowing()
        {
            Assert.DoesNotThrow(() =>
            {
                _audioManager.StartBowDraw(Vector3.zero);
                _audioManager.UpdateBowDrawPosition(Vector3.forward);
                _audioManager.StopBowDraw();
                _audioManager.PlayBowRelease(Vector3.zero, 0.8f);
                _audioManager.PlayArrowHitTarget(Vector3.one);
                _audioManager.PlaySurfaceHit(Vector3.one);
                _audioManager.PlayTargetAction(Vector3.one);
                _audioManager.PlayUiClick();
            });
        }
    }
}
