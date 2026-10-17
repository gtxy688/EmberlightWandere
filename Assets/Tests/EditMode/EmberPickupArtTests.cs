using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberPickupArtTests
    {
        const string Path = "Assets/Resources/Art/Pickups/ember-pickups.png";

        [Test]
        public void PickupAtlas_ProvidesThreePersistentCenteredSprites()
        {
            var sprites = Resources.LoadAll<Sprite>("Art/Pickups/ember-pickups");
            Assert.That(sprites.Select(sprite => sprite.name), Is.EquivalentTo(new[] { "pickup-heal", "pickup-power", "pickup-haste" }));
            foreach (var sprite in sprites)
            {
                Assert.That(EditorUtility.IsPersistent(sprite), Is.True);
                Assert.That(sprite.pivot, Is.EqualTo(sprite.rect.size * .5f));
            }
        }

        [Test]
        public void PickupSource_HasTransparentCornersInsteadOfPaintedBackground()
        {
            Assert.That(System.IO.File.Exists(Path), Is.True);
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes(Path)), Is.True);
                foreach (var corner in new[] { Vector2Int.zero, new Vector2Int(texture.width - 1, 0),
                    new Vector2Int(0, texture.height - 1), new Vector2Int(texture.width - 1, texture.height - 1) })
                    Assert.That(texture.GetPixel(corner.x, corner.y).a, Is.LessThan(.05f));
            }
            finally { Object.DestroyImmediate(texture); }
        }
    }
}
