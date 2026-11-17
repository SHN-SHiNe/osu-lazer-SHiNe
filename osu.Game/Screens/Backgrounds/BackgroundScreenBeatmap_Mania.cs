using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Screens.Backgrounds;
using osuTK.Graphics;

namespace osu.Game.Screens.Backgrounds
{
    /// <summary>
    /// A background screen specifically designed for Mania mode with stage area masking.
    /// </summary>
    public partial class BackgroundScreenBeatmap_Mania : BackgroundScreenBeatmap
    {
        private object maniaPlayfield;

        public BackgroundScreenBeatmap_Mania(WorkingBeatmap beatmap = null, object playfield = null)
            : base(beatmap)
        {
            maniaPlayfield = playfield;
        }

        public object ManiaPlayfield
        {
            get => maniaPlayfield;
            set
            {
                maniaPlayfield = value;
                updateStageMask();
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateStageMask();
        }

        private void updateStageMask()
        {
            if (maniaPlayfield == null) return;

            try
            {
                // Use reflection to access ManiaPlayfield properties
                var playfieldType = maniaPlayfield.GetType();
                var skinnableComponentProperty = playfieldType.GetProperty("SkinnableComponentScreenSpaceDrawQuad");

                if (skinnableComponentProperty != null)
                {
                    // Calculate the stage area bounds
                    var stageBounds = (Quad)skinnableComponentProperty.GetValue(maniaPlayfield);

                    // Convert screen space bounds to local space
                    var localBounds = ToLocalSpace(stageBounds);

                    // Update the mask container to only show the stage area
                    Masking = true;

                    // Set the mask to the stage area
                    Position = localBounds.TopLeft;
                    Size = localBounds.Size;
                }
            }
            catch
            {
                // If reflection fails, disable masking
                Masking = false;
            }
        }
    }
}
