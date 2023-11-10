#if UNITASK
namespace GameMode.VContainer
{
    using Cysharp.Threading.Tasks.Triggers;

    public partial class SceneContext
    {
        public AsyncApplicationFocusTrigger OnFocus() => this.GetAsyncApplicationFocusTrigger();
    }
}
#endif
