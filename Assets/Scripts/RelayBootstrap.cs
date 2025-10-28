using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using FishNet.Managing;
using FishNet.Transporting.UTP;         // UnityTransport для FishNet (Fishy UTP)
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelayBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager fishNet;
    [SerializeField] private Button createServerButton; // Host / Stop Host
    [SerializeField] private Button joinServerButton;   // Join / Disconnect
    [SerializeField] private TMP_InputField codeInputField;

    private enum NetMode { Offline, Hosting, Client }
    private NetMode _mode = NetMode.Offline;

    private bool _servicesReady;

    private void Awake()
    {
        // Гарантируем подписки ровно один раз
        createServerButton.onClick.RemoveAllListeners();
        joinServerButton.onClick.RemoveAllListeners();

        createServerButton.onClick.AddListener(OnClickHostToggle);
        joinServerButton.onClick.AddListener(OnClickJoinToggle);

        UpdateUi();
    }

    private async Task EnsureServicesAsync()
    {
        if (_servicesReady) return;

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        _servicesReady = true;
    }

    private async void OnClickHostToggle()
    {
        try
        {
            if (_mode != NetMode.Hosting)
            {
                // Стартуем хост и показываем код
                string code = await StartHostRelayAsync(7);
                codeInputField.text = code;
                _mode = NetMode.Hosting;
            }
            else
            {
                // Останавливаем хост
                StopHost();
                _mode = NetMode.Offline;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            // При ошибке возвращаемся в офлайн
            _mode = NetMode.Offline;
        }

        UpdateUi();
    }

    private async void OnClickJoinToggle()
    {
        try
        {
            if (_mode != NetMode.Client)
            {
                string joinCode = codeInputField.text.Trim();
                if (string.IsNullOrEmpty(joinCode))
                {
                    Debug.LogWarning("Введите join code для подключения к хосту.");
                    return;
                }

                bool ok = await StartClientRelayAsync(joinCode);
                if (ok)
                    _mode = NetMode.Client;
                else
                    Debug.LogWarning("Не удалось подключиться к хосту через Relay.");
            }
            else
            {
                StopClient();
                _mode = NetMode.Offline;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            _mode = NetMode.Offline;
        }

        UpdateUi();
    }

    private void UpdateUi()
    {
        // Тексты на кнопках
        SetButtonText(createServerButton, _mode == NetMode.Hosting ? "Stop Host" : "Host (Relay)");
        SetButtonText(joinServerButton,    _mode == NetMode.Client  ? "Disconnect" : "Join (Relay)");

        // Поведение поля ввода кода:
        // - когда мы ХОСТ — поле заполняется кодом и блокируется (копировать можно, редактировать — нет)
        // - когда не хост — поле активно для ввода
        bool isHosting = _mode == NetMode.Hosting;
        codeInputField.interactable = !isHosting;

        // Кнопки:
        // - нельзя нажать Join, когда мы уже хост (и наоборот — можно, если хочешь, но обычно нет смысла)
        joinServerButton.interactable = _mode != NetMode.Hosting;

        // Дополнительно можно серым делать host-кнопку при Client, но оставим возможность переключиться
        createServerButton.interactable = _mode != NetMode.Client;
    }

    private void SetButtonText(Button btn, string text)
    {
        // Поддержка и TextMeshPro, и обычного Text
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) { tmp.text = text; return; }

        var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>();
        if (uiText != null) uiText.text = text;
    }

    // ========================= Relay + FishNet =========================

    public async Task<string> StartHostRelayAsync(int maxConnections = 7)
    {
        await EnsureServicesAsync();

        var alloc    = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        var utp = fishNet.TransportManager.GetTransport<UnityTransport>();
        utp.SetRelayServerData(
            alloc.RelayServer.IpV4,
            (ushort)alloc.RelayServer.Port,
            alloc.AllocationIdBytes,
            alloc.Key,
            alloc.ConnectionData,
            alloc.ConnectionData,   // у хоста hostConnectionData = свои же connectionData
            true                    // DTLS
        );

        // Host = сервер + локальный клиент
        fishNet.ServerManager.StartConnection();
        fishNet.ClientManager.StartConnection();

        return joinCode;
    }

    public async Task<bool> StartClientRelayAsync(string joinCode)
    {
        await EnsureServicesAsync();

        var join = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var utp = fishNet.TransportManager.GetTransport<UnityTransport>();
        utp.SetRelayServerData(
            join.RelayServer.IpV4,
            (ushort)join.RelayServer.Port,
            join.AllocationIdBytes,
            join.Key,
            join.ConnectionData,
            join.HostConnectionData, // ВАЖНО у клиента — данные хоста
            true
        );

        return fishNet.ClientManager.StartConnection();
    }

    private void StopHost()
    {
        // порядок не критичен, но обычно сначала клиент, потом сервер
        if (fishNet.ClientManager.Connection.ClientId != -1) // подключён?
            fishNet.ClientManager.StopConnection();

        if (fishNet.ServerManager.Started)
            fishNet.ServerManager.StopConnection(true);

        // очищаем поле и делаем его снова редактируемым
        codeInputField.text = string.Empty;
        codeInputField.interactable = true;
    }

    private void StopClient()
    {
        if (fishNet.ClientManager.Connection.ClientId != -1)
            fishNet.ClientManager.StopConnection();

        // при дисконнекте клиента поле остаётся активным для ввода
        codeInputField.interactable = true;
    }
}
