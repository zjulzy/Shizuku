using Shizuku.Graph;

namespace Shizuku.Tests.PlayMode
{
    public sealed class BlueprintLifecycleTestGraph : ShizukuBluePrint<BlueprintLifecycleTestBehavior>
    {
        public int InitializeBehaviorCount;

        public override void InitializeBehavior(BlueprintLifecycleTestBehavior behavior)
        {
            InitializeBehaviorCount++;
            base.InitializeBehavior(behavior);
        }
    }
}
