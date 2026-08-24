using RichHudFramework;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class RotatedTexture : HudElementBase
    {
        #region State and Construction

        private readonly bool reverse;
        private BoundedQuadMaterial material;

        public Color Color
        {
            set { material.bbColor = value.GetBbColor(); }
        }

        public RotatedTexture(string materialSubtype, bool reverse, HudParentBase parent = null)
            : this(new Material(materialSubtype, Vector2.One), reverse, parent)
        {
        }

        public RotatedTexture(Material source, bool reverse, HudParentBase parent = null) : base(parent)
        {
            this.reverse = reverse;
            material = BoundedQuadMaterial.Default;
            material.textureID = source.TextureID;
            material.texBounds = source.UVBounds;
            Color = Color.White;
        }

        #endregion

        #region Rendering

        protected override void Draw()
        {
            Vector2 halfSize = UnpaddedSize * .5f;
            Vector2 topRight = Position + halfSize;
            Vector2 bottomRight = Position + new Vector2(halfSize.X, -halfSize.Y);
            Vector2 bottomLeft = Position - halfSize;
            Vector2 topLeft = Position + new Vector2(-halfSize.X, halfSize.Y);

            FlatQuad quad = reverse
                ? new FlatQuad(bottomLeft, topLeft, topRight, bottomRight)
                : new FlatQuad(topRight, bottomRight, bottomLeft, topLeft);
            BillBoardUtils.AddQuad(ref quad, ref material, HudSpace.PlaneToWorldRef);
        }

        #endregion
    }
}
