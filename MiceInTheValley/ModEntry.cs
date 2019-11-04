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
        // Track the mice added to the list of critters.
        private Dictionary<string, List<Critter>> mice_ = new Dictionary<string, List<Critter>>();

        // Squeak.
        private SoundEffect sound_;

        public override void Entry(IModHelper helper) {
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding  += OnDayEnding;

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

        // Mice are added once a day.
        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            Monitor.Log("OnDayStarted()");

            foreach (var location in Game1.locations) {
                // Prepare tracking list.
                if (!mice_.ContainsKey(location.Name)) {
                    mice_.Add(location.Name, new List<Critter>());
                }

                #if false
                // Just for debugging.
                var crittersField = this.Helper.Reflection.GetField<List<Critter>>(location, "critters");
                List<Critter> critters = crittersField.GetValue();
                if (critters != null) {
                    Monitor.Log($"Number of critters in {location.Name}: {critters.Count}");
                    foreach (var critter in critters) {
                        Monitor.Log($"Critter in {location.Name}: {critter.GetType()}");
                    }
                }
                else {
                    Monitor.Log($"List of critters in {location.Name} is null");
                }
                #endif

                // Add at most 10 mice per location.
                for (int i = 0; i < 10; ++i) {
                    addMice(location, 0.5);
                }
            }
        }

        // Remove remaining mice at the end of day so the town doesn't get infested over time.
        private void OnDayEnding(object sender, DayEndingEventArgs e) {
            Monitor.Log("OnDayEnding()");

            foreach (var location in Game1.locations) {
                var crittersField = this.Helper.Reflection.GetField<List<Critter>>(location, "critters");
                List<Critter> critters = crittersField.GetValue();
                if (critters != null) {
                    // Remove the remaining mice.
                    foreach (var mouse in mice_[location.Name]) {
                        bool success = critters.Remove(mouse);
                        if (success) {
                            Monitor.Log($"Removed mouse from {location.Name}");
                        }
                    }
                }
                // Clear tracking list.
                mice_[location.Name].Clear();
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
                        position += direction;
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
            // Add to tracking list.
            mice_[location.Name].Add(mouse);

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
