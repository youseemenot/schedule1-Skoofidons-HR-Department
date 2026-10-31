using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.State;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Skoofidons.SharedSettings.V1
{
    public sealed class GameUiInputLease : IMenuInputLease
    {
        private GameObject root;
        private MonoState state;
        private SceneState parent;
        private GameInput.ExitDelegate exit;
        private bool acquired, removed, unhooked, complete;
        private readonly Action close;
        public bool IsAlive => acquired && root != null && state != null && parent != null &&
            SceneState.Current != null && parent.Pointer == SceneState.Current.Pointer;
        public bool IsTop
        {
            get { if (!IsAlive) return false; var top = parent.Peek(); return top != null && top.Pointer == state.Pointer; }
        }
        public GameUiInputLease(Action close)
        {
            this.close = close;
            parent = SceneState.Current;
            if (parent == null) throw new InvalidOperationException("Scene UI state is not ready.");
            try
            {
                root = new GameObject("Skoofidons.SharedSettings.Input");
                root.SetActive(false); root.transform.SetParent(parent.transform, false);
                state = root.AddComponent<MonoState>();
                state._autoSetupExitListeners = false; state._customStateProperties = true;
                var props = StateProperties.GetPreset(StateProperties.EPreset.UIDefault);
                props.MouseState = StateProperties.EMouseState.Free;
                props.Movement = StateProperties.EMovementState.Locked;
                props.CameraLook = StateProperties.ELookState.Locked;
                props.Equipping = StateProperties.EEquippingState.Disabled;
                props.Inventory = StateProperties.EInventoryState.Disabled;
                state.Properties = props; state.InitializeDefaultParent(parent); root.SetActive(true);
                exit = (Action<ExitAction>)OnExit;
                GameInput.RegisterExitListener(exit, int.MaxValue);
                state.PushToDefaultParent(); acquired = true;
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Warning("[Shared F3] Input acquisition failed: " + e.Message);
                // Return the retained, non-live lease to MenuSession for ordered retry cleanup.
            }
        }
        private void OnExit(ExitAction action)
        {
            if (!IsTop || action.Used || action.Type != ExitType.Primary) return;
            action.Use(); close();
        }
        public bool TryRelease(out string error)
        {
            error = null; if (complete) return true;
            try
            {
                if (!removed)
                {
                    if (parent != null && !ReferenceEquals(state, null)) state.RemoveFromDefaultParent();
                    removed = true;
                }
                if (!unhooked)
                {
                    if (exit != null) GameInput.DeregisterExitListener(exit);
                    exit = null; unhooked = true;
                }
                if (root != null) Object.Destroy(root);
                root = null; state = null; parent = null; acquired = false; complete = true;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}
