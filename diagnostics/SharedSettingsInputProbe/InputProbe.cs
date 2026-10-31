using System;
using System.IO;
using System.Security.Cryptography;
using Il2CppScheduleOne;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.State;
using MelonLoader;
using MelonLoader.Utils;
using Skoofidons.SharedSettings.V1;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(Skoofidons.SettingsProbe.InputProbe), "Skoofidon's Settings Input Probe", "0.1.1", "Skoofidon")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace Skoofidons.SettingsProbe
{
    // Temporary input proof only. No save, employee, machine or Minimap changes.
    public sealed class InputProbe : MelonMod
    {
        private GameObject root;
        private MonoState state;
        private SceneState parent;
        private GameInput.ExitDelegate exit;
        private Il2CppSystem.Action<bool> focusChanged;
        private readonly ReleaseBarrier barrier = new();
        private readonly ProbeCleanupCoordinator cleanup = new();
        private bool closing, failed, cleanupErrorLogged;
        private int clicks, samples, openFrame;
        private string owningScene;
        private string status = "Press the test button, move the mouse, try WASD.";

        public override void OnInitializeMelon()
        {
            const string expected = "872DFB646C9B866C53DAE12D7A34E1F9DF21A3E16FAC6B7C6CD21BF5FC8E4B3F99932163C5DE7E3798D37641712E82B914D46E1F023D56E77970BE84FB74EC3A";
            using var stream = File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll"));
            using var sha = SHA512.Create();
            failed = Convert.ToHexString(sha.ComputeHash(stream)) != expected;
            if (failed) MelonLogger.Warning("[F3 INPUT PROBE INACTIVE] Game binary differs from the inspected build.");
            else
            {
                focusChanged = (Action<bool>)OnFocusChanged;
                Application.focusChanged += focusChanged;
                MelonLogger.Msg("[F3 INPUT PROBE READY] Temporary F6 panel; F3/F4 remain untouched. No settings are changed.");
            }
        }

        private void OnFocusChanged(bool focused) { if (!focused) RequestClose("focus lost"); }

        private bool IsTop()
        {
            if (state == null || parent == null) return false;
            var top = parent.Peek();
            return top != null && top.Pointer == state.Pointer;
        }

        private void Open()
        {
            if (failed || root != null || exit != null || cleanup.Pending) return;
            parent = SceneState.Current;
            if (parent == null) { MelonLogger.Warning("[F3 INPUT PROBE] SceneState not ready."); return; }
            try
            {
                cleanup.Reset();
                cleanupErrorLogged = false;
                owningScene = parent.gameObject.scene.name;
                root = new GameObject("Skoofidons.Settings.InputProbe");
                root.SetActive(false);
                root.transform.SetParent(parent.transform, false);
                state = root.AddComponent<MonoState>();
                state._autoSetupExitListeners = false;
                state._customStateProperties = true;
                var properties = StateProperties.GetPreset(StateProperties.EPreset.UIDefault);
                properties.MouseState = StateProperties.EMouseState.Free;
                properties.Movement = StateProperties.EMovementState.Locked;
                properties.CameraLook = StateProperties.ELookState.Locked;
                properties.Equipping = StateProperties.EEquippingState.Disabled;
                properties.Inventory = StateProperties.EInventoryState.Disabled;
                state.Properties = properties;
                state.InitializeDefaultParent(parent);
                root.SetActive(true);
                exit = (Action<ExitAction>)OnExit;
                GameInput.RegisterExitListener(exit, int.MaxValue);
                state.PushToDefaultParent();
                closing = false;
                barrier.Reset();
                samples = 0;
                openFrame = Time.frameCount;
                MelonLogger.Msg("[F3 INPUT PROBE OPEN] frame=" + openFrame + "; owned root state, default UI properties.");
            }
            catch (Exception error)
            {
                failed = true;
                MelonLogger.Error("[F3 INPUT PROBE OPEN FAILED] " + error);
                Cleanup();
            }
        }

        private void OnExit(ExitAction action)
        {
            if (state == null || !IsTop() || action.Used || action.Type != ExitType.Primary) return;
            action.Use();
            RequestClose("native ExitAction");
        }

        private void RequestClose(string reason)
        {
            if (state == null || closing) return;
            closing = true;
            barrier.Begin(Time.frameCount);
            MelonLogger.Msg("[F3 INPUT PROBE CLOSING] " + reason + "; frame=" + Time.frameCount);
        }

        public override void OnUpdate()
        {
            try
            {
                if (cleanup.Pending) { Cleanup(); return; }
                if (exit != null && (root == null || state == null || parent == null)) Cleanup();
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    if (state == null) Open();
                    else RequestClose("F6");
                }
                if (state == null) return;
                if (SceneState.Current == null || parent == null || SceneState.Current.Pointer != parent.Pointer)
                { Cleanup(); return; }
                if (!Application.isFocused) RequestClose("focus lost");
                if (closing && barrier.Ready(Time.frameCount, Input.anyKey, IsTop(), Application.isFocused))
                    Cleanup();
                if (state != null && samples < 4 && Time.frameCount <= openFrame + 3)
                {
                    samples++;
                    MelonLogger.Msg("[F3 INPUT PROBE SAMPLE] frame=" + Time.frameCount + "; top=" + IsTop() +
                        "; mouseVisible=" + Cursor.visible + "; lock=" + Cursor.lockState +
                        "; canLook=" + (PlayerCamera.Instance != null ? PlayerCamera.Instance.CanLook.ToString() : "n/a") +
                        "; canMove=" + (PlayerMovement.Instance != null ? PlayerMovement.Instance.CanMove.ToString() : "n/a"));
                }
            }
            catch (Exception error)
            {
                failed = true;
                MelonLogger.Error("[F3 INPUT PROBE UPDATE FAILED] " + error);
                Cleanup();
            }
        }

        public override void OnGUI()
        {
            if (state == null || closing || !IsTop()) return;
            try
            {
                var x = Math.Max(10, (Screen.width - 500) / 2);
                var y = Math.Max(10, (Screen.height - 260) / 2);
                GUI.Box(new Rect(x, y, 500, 260), "Shared settings: input proof (F6)");
                GUI.Label(new Rect(x + 20, y + 40, 460, 45), status);
                if (GUI.Button(new Rect(x + 20, y + 95, 220, 40), "Test click: " + clicks))
                { clicks++; status = "UI click #" + clicks + " received. Check that the world did not react."; }
                if (GUI.Button(new Rect(x + 260, y + 95, 220, 40), "Close"))
                    RequestClose("UI button");
                GUI.Label(new Rect(x + 20, y + 150, 460, 75),
                    "Try: mouse look, WASD, jump, interaction, item use.\nEscape/F6 closes only this panel.\nHeld input keeps the close barrier until released.");
            }
            catch (Exception error)
            {
                failed = true;
                MelonLogger.Error("[F3 INPUT PROBE GUI FAILED] " + error);
                RequestClose("GUI failure");
            }
        }

        private void Cleanup()
        {
            if (root == null && state == null && exit == null)
            { parent = null; closing = false; barrier.Reset(); return; }
            bool ownerAlive = parent != null;
            bool done = cleanup.TryComplete(ownerAlive,
                () => { if (!ReferenceEquals(state, null)) state.RemoveFromDefaultParent(); },
                () => { if (exit != null) GameInput.DeregisterExitListener(exit); exit = null; },
                () => { if (root != null) Object.Destroy(root); }, out Exception error);
            if (!done)
            {
                closing = true;
                barrier.Begin(Time.frameCount);
                if (!cleanupErrorLogged)
                {
                    cleanupErrorLogged = true;
                    MelonLogger.Warning("[F3 INPUT PROBE CLEANUP RETRY] " + error.Message);
                }
                return;
            }
            root = null; state = null; parent = null; closing = false; barrier.Reset();
            owningScene = null;
            cleanupErrorLogged = false;
            MelonLogger.Msg("[F3 INPUT PROBE CLOSED] Owned resources released; ownerWasAlive=" + ownerAlive);
        }

        private void Shutdown()
        {
            try { if (focusChanged != null) Application.focusChanged -= focusChanged; }
            finally { focusChanged = null; Cleanup(); }
        }
        public override void OnApplicationQuit() => Shutdown();
        public override void OnDeinitializeMelon() => Shutdown();
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (parent == null || SceneState.Current == null || parent.Pointer != SceneState.Current.Pointer || owningScene == sceneName)
                Cleanup();
        }
    }
}
