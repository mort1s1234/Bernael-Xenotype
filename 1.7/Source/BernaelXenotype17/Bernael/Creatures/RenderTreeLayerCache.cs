using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    internal sealed class BodyHeadNodePair
    {
        public PawnRenderNode BodyNode;
        public PawnRenderNode HeadNode;
    }

    internal static class RenderTreeLayerCache
    {
        private static readonly ConditionalWeakTable<PawnRenderTree, BodyHeadNodePair> Cache = new();

        public static BodyHeadNodePair GetBodyAndHead(PawnRenderTree tree)
        {
            if (tree == null || tree.rootNode == null) return null;
            if (Cache.TryGetValue(tree, out BodyHeadNodePair existing))
                return existing;

            var pair = new BodyHeadNodePair();
            var stack = new Stack<PawnRenderNode>();
            stack.Push(tree.rootNode);

            while (stack.Count > 0)
            {
                PawnRenderNode current = stack.Pop();
                if (current == null) continue;

                bool isBodyWorker = current.Worker is PawnRenderNodeWorker_Body ||
                                    current.Worker is PawnRenderNodeWorker_AnimalBody;
                bool isBodyTag = current.Props?.tagDef == PawnRenderNodeTagDefOf.Body ||
                                 current.Props?.tagDef?.defName == "Body";

                if (pair.BodyNode == null && (isBodyWorker || isBodyTag))
                    pair.BodyNode = current;

                bool isHeadWorker = current.Worker is PawnRenderNodeWorker_Head;
                bool isHeadTag = current.Props?.tagDef == PawnRenderNodeTagDefOf.Head ||
                                 current.Props?.tagDef?.defName == "Head";

                if (pair.HeadNode == null && (isHeadWorker || isHeadTag))
                    pair.HeadNode = current;

                if (pair.BodyNode != null && pair.HeadNode != null)
                    break;

                if (current.children != null)
                {
                    for (int i = 0; i < current.children.Length; i++)
                        stack.Push(current.children[i]);
                }
            }

            Cache.Add(tree, pair);
            return pair;
        }
    }
}
