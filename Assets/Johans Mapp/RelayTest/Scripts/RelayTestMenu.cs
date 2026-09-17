using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Johans.RelayTest
{
    // Uses the same direct Relay flow as the old project's RelayManager.
    public class RelayTestMenu : MonoBehaviour
    {
        public NetworkManager Manager;
        public InputField PlayerNameInput;
        public InputField JoinCodeInput;
        public Text StatusText;
        public Button HostButton, JoinButton, LeaveButton, CopyButton;
        public static string LocalPlayerName { get; private set; } = "Player";
        private bool busy;
        private string hostCode = "";
        private float connectionDeadline;

        private void Awake()
        {
            Application.runInBackground = true;
            HostButton.onClick.AddListener(Host);
            JoinButton.onClick.AddListener(Join);
            LeaveButton.onClick.AddListener(Leave);
            CopyButton.onClick.AddListener(() => GUIUtility.systemCopyBuffer = hostCode);
            Manager.OnClientConnectedCallback += Connected;
            Manager.OnClientDisconnectCallback += Disconnected;
            SetStatus("Skriv namn och välj Host eller ange kod och välj Join.");
        }

        private void Update()
        {
            bool idle = !busy && !Manager.IsListening && !Manager.ShutdownInProgress;
            HostButton.interactable = JoinButton.interactable = idle;
            PlayerNameInput.interactable = idle;
            JoinCodeInput.readOnly = !idle;
            LeaveButton.interactable = Manager.IsListening && !Manager.ShutdownInProgress;
            CopyButton.interactable = Manager.IsHost && hostCode.Length > 0;
            if (connectionDeadline > 0 && Time.realtimeSinceStartup > connectionDeadline)
            {
                connectionDeadline = 0;
                Manager.Shutdown();
                SetStatus("Anslutningen tog för lång tid. Kontrollera koden och att hosten är kvar.");
            }
        }

        public async void Host() => await Connect(true);
        public async void Join() => await Connect(false);

        private async Task Connect(bool host)
        {
            if (busy || Manager.IsListening || Manager.ShutdownInProgress) return;
            string code = JoinCodeInput.text.Trim().ToUpperInvariant();
            if (!host && code.Length == 0) { SetStatus("Skriv hostens joinkod först."); return; }
            LocalPlayerName = PlayerNameInput.text.Trim();
            if (LocalPlayerName.Length == 0) LocalPlayerName = "Player";
            busy = true;
            SetStatus("Ansluter till Unity Services...");
            var lifetime = destroyCancellationToken;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();
                if (lifetime.IsCancellationRequested) return;
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    // Stable per process, distinct for Editor, additional Editors and builds.
                    // Test identities only: do not use this profile strategy for a released game.
                    AuthenticationService.Instance.SwitchProfile("relaytest_" + System.Diagnostics.Process.GetCurrentProcess().Id);
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                if (lifetime.IsCancellationRequested) return;
                var transport = Manager.GetComponent<UnityTransport>();
                if (host)
                {
                    var allocation = await RelayService.Instance.CreateAllocationAsync(3);
                    if (lifetime.IsCancellationRequested) return;
                    string newCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    if (lifetime.IsCancellationRequested) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    if (!Manager.StartHost()) throw new InvalidOperationException("NetworkManager kunde inte starta host.");
                    hostCode = newCode;
                    JoinCodeInput.text = hostCode;
                    SetStatus("Host är igång! Dela koden ovan. Max 4 spelare. WASD / piltangenter flyttar dig.");
                }
                else
                {
                    var allocation = await RelayService.Instance.JoinAllocationAsync(code);
                    if (lifetime.IsCancellationRequested) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    if (!Manager.StartClient()) throw new InvalidOperationException("NetworkManager kunde inte starta klienten.");
                    connectionDeadline = Time.realtimeSinceStartup + 20;
                    SetStatus("Kontaktar hosten...");
                }
            }
            catch (Exception exception)
            {
                if (lifetime.IsCancellationRequested) return;
                connectionDeadline = 0;
                hostCode = "";
                Manager.Shutdown();
                SetStatus("Kunde inte ansluta: " + exception.Message);
                Debug.LogException(exception, this);
            }
            finally { busy = false; }
        }

        private void Connected(ulong clientId)
        {
            if (clientId != Manager.LocalClientId) return;
            connectionDeadline = 0;
            if (!Manager.IsHost) SetStatus("Ansluten! WASD / piltangenter flyttar dig. Klicka i spelvärlden först.");
        }

        private void Disconnected(ulong clientId)
        {
            if (Manager.IsServer || clientId != Manager.LocalClientId) return;
            connectionDeadline = 0;
            SetStatus("Frånkopplad från hosten. " + Manager.DisconnectReason);
        }

        public void Leave()
        {
            connectionDeadline = 0;
            hostCode = "";
            Manager.Shutdown();
            SetStatus("Lämnat spelet. Starta en ny host eller gå med igen.");
        }

        private void SetStatus(string message) => StatusText.text = message;

        private void OnDestroy()
        {
            if (!Manager) return;
            Manager.OnClientConnectedCallback -= Connected;
            Manager.OnClientDisconnectCallback -= Disconnected;
        }
    }
}
