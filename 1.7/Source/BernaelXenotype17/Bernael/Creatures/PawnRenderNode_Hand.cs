using UnityEngine;
using Verse;

namespace Bernael_Xenotype
{
    public class PawnRenderNodeProperties_Hand : PawnRenderNodeProperties
    {
        public bool isLeftHand = false;
        public bool hideWhenDead = false;
        public float deadLayerOffset = 0.15f;
        public Color fallbackTint = Color.white;

        public Vector3 northOffset = Vector3.zero;
        public Vector3 eastOffset = Vector3.zero;
        public Vector3 southOffset = Vector3.zero;
        public Vector3 westOffset = Vector3.zero;

        public Vector3 northSwingDeltaOffset = Vector3.zero;
        public Vector3 eastSwingDeltaOffset = Vector3.zero;
        public Vector3 southSwingDeltaOffset = Vector3.zero;
        public Vector3 westSwingDeltaOffset = Vector3.zero;

        public float northSwingAngleDeg = 0f;
        public float eastSwingAngleDeg = 0f;
        public float southSwingAngleDeg = 0f;
        public float westSwingAngleDeg = 0f;

        public Vector3 northCastRaiseOffset = new Vector3(0f, 0f, 0.5f);
        public Vector3 eastCastRaiseOffset = new Vector3(0f, 0f, 0.5f);
        public Vector3 southCastRaiseOffset = new Vector3(0f, 0f, 0.5f);
        public Vector3 westCastRaiseOffset = new Vector3(0f, 0f, 0.5f);

        public float northCastRaiseAngleDeg = 0f;
        public float eastCastRaiseAngleDeg = 0f;
        public float southCastRaiseAngleDeg = 0f;
        public float westCastRaiseAngleDeg = 0f;

        public PawnRenderNodeProperties_Hand()
        {
            nodeClass = typeof(PawnRenderNode_Hand);
            workerClass = typeof(PawnRenderNodeWorker_Hand);
        }

        internal Vector3 BaseOffset(Rot4 facing) =>
            Select(facing, northOffset, eastOffset, southOffset, westOffset);
        internal Vector3 SwingOffset(Rot4 facing) =>
            Select(facing, northSwingDeltaOffset, eastSwingDeltaOffset, southSwingDeltaOffset, westSwingDeltaOffset);
        internal Vector3 CastRaiseOffset(Rot4 facing) =>
            Select(facing, northCastRaiseOffset, eastCastRaiseOffset, southCastRaiseOffset, westCastRaiseOffset);

        internal float SwingAngle(Rot4 facing) =>
            Select(facing, northSwingAngleDeg, eastSwingAngleDeg, southSwingAngleDeg, westSwingAngleDeg);
        internal float CastRaiseAngle(Rot4 facing) =>
            Select(facing, northCastRaiseAngleDeg, eastCastRaiseAngleDeg, southCastRaiseAngleDeg, westCastRaiseAngleDeg);

        private static T Select<T>(Rot4 facing, T north, T east, T south, T west) =>
            facing == Rot4.North ? north :
            facing == Rot4.East  ? east  :
            facing == Rot4.West  ? west  : south;
    }

    public class PawnRenderNode_Hand : PawnRenderNode
    {
        public new PawnRenderNodeProperties_Hand Props => (PawnRenderNodeProperties_Hand)props;

        public PawnRenderNode_Hand(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { }

        public override Graphic GraphicFor(Pawn pawn)
        {
            if (string.IsNullOrEmpty(Props.texPath)) return null;
            return GraphicDatabase.Get<Graphic_Multi>(
                Props.texPath,
                ShaderDatabase.Cutout,
                Props.drawSize.magnitude > 0.0001f ? Props.drawSize : Vector2.one,
                pawn?.story != null ? pawn.story.SkinColor : Props.fallbackTint
            );
        }
    }

    public class PawnRenderNodeWorker_Hand : PawnRenderNodeWorker
    {
        private static bool IsFarHand(Rot4 facing, bool isLeftHand) =>
            (facing == Rot4.East && isLeftHand) ||
            (facing == Rot4.West && !isLeftHand) ||
            facing == Rot4.North;

        private static Vector3 ScaleHandOffset(Vector3 delta, float weight, bool isFarHand) =>
            new(delta.x * weight, isFarHand ? 0f : delta.y * weight, delta.z * weight);

        private static Quaternion AngleAxisForHand(float angleDeg, float weight, Rot4 facing, bool isLeftHand)
        {
            if (Mathf.Approximately(angleDeg * weight, 0f)) return Quaternion.identity;
            float handedSign = isLeftHand ? -1f : 1f;
            float facingSign = (facing == Rot4.West || facing == Rot4.North) ? -1f : 1f;
            float deltaAngle = angleDeg * weight * handedSign * facingSign;
            if (facing == Rot4.East || facing == Rot4.North) deltaAngle = -deltaAngle;
            return Quaternion.AngleAxis(deltaAngle, Vector3.up);
        }

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (node is PawnRenderNode_Hand handNode && parms.pawn is { Dead: true } && handNode.Props.hideWhenDead)
                return false;
            return base.CanDrawNow(node, parms);
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            if (node is not PawnRenderNode_Hand handNode || parms.pawn == null)
                return base.OffsetFor(node, parms, out pivot);

            var props = handNode.Props;
            bool farHand = IsFarHand(parms.facing, props.isLeftHand);
            Vector3 screenPos = ScaleHandOffset(props.BaseOffset(parms.facing), 1f, farHand);

            int now = Find.TickManager?.TicksGame ?? 0;
            if (HandAnimTracker.TryGet(parms.pawn, out HandAnimState anim))
            {
                if (anim.Swing.IsActive(parms.pawn, now))
                {
                    float pulse = Mathf.Sin(Mathf.PI * anim.Swing.Progress(now));
                    screenPos += ScaleHandOffset(props.SwingOffset(parms.facing), pulse, farHand);
                }
                if (anim.CastRaise.IsActive(parms.pawn, now))
                {
                    float weight = Mathf.SmoothStep(0f, 1f, anim.CastRaise.Progress(now));
                    screenPos += ScaleHandOffset(props.CastRaiseOffset(parms.facing), weight, farHand);
                }
            }

            return base.OffsetFor(node, parms, out pivot) + screenPos;
        }

        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            if (node is not PawnRenderNode_Hand handNode || parms.pawn == null || node.tree == null)
                return base.LayerFor(node, parms);

            BodyHeadNodePair pair = RenderTreeLayerCache.GetBodyAndHead(node.tree);
            float bodyLayer = pair?.BodyNode?.Worker?.LayerFor(pair.BodyNode, parms) ?? 0f;
            float headLayer = pair?.HeadNode?.Worker?.LayerFor(pair.HeadNode, parms) ?? bodyLayer;
            float topBodyHead = Mathf.Max(bodyLayer, headLayer);
            if (parms.pawn.Dead)
            {
                float deadOffset = Mathf.Max(0f, handNode.Props.deadLayerOffset);
                return Mathf.Min(100f, topBodyHead + deadOffset);
            }
            const float margin = 1.0f;
            return IsFarHand(parms.facing, handNode.Props.isLeftHand)
                ? Mathf.Max(-10f, Mathf.Min(bodyLayer, headLayer) - margin)
                : Mathf.Min(100f, topBodyHead + margin);
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            if (node is not PawnRenderNode_Hand handNode || parms.pawn == null)
                return base.RotationFor(node, parms);

            Quaternion rot = base.RotationFor(node, parms);
            int now = Find.TickManager?.TicksGame ?? 0;
            if (!HandAnimTracker.TryGet(parms.pawn, out HandAnimState anim))
                return rot;

            var props = handNode.Props;
            if (anim.Swing.IsActive(parms.pawn, now))
            {
                float pulse = Mathf.Sin(Mathf.PI * anim.Swing.Progress(now));
                rot *= AngleAxisForHand(props.SwingAngle(parms.facing), pulse, parms.facing, props.isLeftHand);
            }
            if (anim.CastRaise.IsActive(parms.pawn, now))
            {
                float weight = Mathf.SmoothStep(0f, 1f, anim.CastRaise.Progress(now));
                rot *= AngleAxisForHand(props.CastRaiseAngle(parms.facing), weight, parms.facing, props.isLeftHand);
            }
            return rot;
        }
    }
}
