using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;


namespace MiceInTheValley {
    public class ModEntry : Mod, IAssetLoader {
        // Squeak.
        private SoundEffect sound_;

        public override void Entry(IModHelper helper) {
            helper.Events.Player.Warped += OnWarped;

            // Load the sound.
            string soundFilePath = Path.Combine(helper.DirectoryPath, "assets/mouse.wav");
            this.Monitor.Log($"Sound file: {soundFilePath}");

            using (Stream fs = new FileStream(soundFilePath, FileMode.Open)) {
                sound_ = SoundEffect.FromStream(fs);
            }
        }

        public bool CanLoad<T>(IAssetInfo asset) {
            bool retval = asset.AssetNameEquals("mouse");
            if (retval) {
                this.Monitor.Log($"Can load asset {asset.AssetName}");
            }

            return retval;
        }

        public T Load<T>(IAssetInfo asset) {
            if (asset.AssetNameEquals("mouse")) {
                this.Monitor.Log($"Load asset {asset.AssetName}");

                return this.Helper.Content.Load<T>("assets/mouse.png");
            }
            else {
                return default(T);
            }
        }

        // Mice are added when warping to a new location.
        private void OnWarped(object sender, WarpedEventArgs e) {
            // Add at most 10 mice per location.
            for (int i = 0; i < 10; ++i) {
                addMice(e.NewLocation, 0.5);
            }
        }

        // Taken from GameLocation.addBunnies() .
        private void addMice(GameLocation location, double chance) {
            if (location is Desert || !(Game1.random.NextDouble() < chance) || location.largeTerrainFeatures == null) {
                return;
            }

            int num = 0;
            Vector2 position;
            Vector2 direction = RandomDirection();
            while (true) {
                if (num >= 3) {
                    // No suitable terrain feature found, give up.
                    return;
                }

                // Pick a random terrain feature and check its usability.
                int index = Game1.random.Next(location.largeTerrainFeatures.Count);
                if (location.largeTerrainFeatures.Count > 0 && location.largeTerrainFeatures[index] is Bush) {
                    position = location.largeTerrainFeatures[index].tilePosition;
                    int num2 = Game1.random.Next(5, 12);
                    bool doIt = true;
                    for (int i = 0; i < num2; i++) {
                        position -= direction;
                        Rectangle rectangle = new Rectangle((int) position.X * 64, (int) position.Y * 64, 64, 64);
                        bool placeable = location.isTileLocationTotallyClearAndPlaceable(position);
                        if (!location.largeTerrainFeatures[index].getBoundingBox().Intersects(rectangle) && !placeable) {
                            doIt = false;

                            break;
                        }
                    }
                    if (doIt) {
                        break;
                    }
                }
                num++;
            }

            int speed = Game1.random.Next(10);
            var mouse = new Mouse(this.Monitor, position, direction, speed, sound_);
            // Add to critters (no reflection necessary to access the list)
            location.addCritter(mouse);

            Monitor.Log($"Added mouse in {location.Name} at {position}");
        }

        // A direction is represented by a signed unit vector in X or Y direction.
        private static Vector2 RandomDirection() {
            Vector2 direction = Game1.random.NextDouble() < 0.5 ? Vector2.UnitY : Vector2.UnitX;
            bool flip = Game1.random.NextDouble() < 0.5;
            if (flip) {
                direction *= -1f;
            }

            return direction;
        }
    }
}
