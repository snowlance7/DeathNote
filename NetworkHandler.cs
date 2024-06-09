using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace DeathNote
{
    public class NetworkHandler : NetworkBehaviour
    {
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;
        public static NetworkHandler Instance { get; private set; }
        public static PlayerControllerB CurrentClient { get { return StartOfRound.Instance.localPlayerController; } }
        
        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            }

            Instance = this;
            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void KillPlayerServerRpc(ulong clientId, string causeOfDeathString, string _details)
        {
            logger.LogDebug("In ServerRpc KillPlayerServerRpc");

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                KillPlayerClientRpc(clientId, causeOfDeathString, _details);
            }
        }

        [ClientRpc]
        private void KillPlayerClientRpc(ulong clientId, string causeOfDeathString, string _details)
        {
            logger.LogDebug("In ClientRpc KillPlayerClientRpc");
            

            if (CurrentClient.actualClientId == clientId)
            {
                PlayerControllerB playerToDie = GameNetworkManager.Instance.localPlayerController;
                CauseOfDeath causeOfDeath = DeathController.GetCauseOfDeathFromString(causeOfDeathString);

                int details = DeathController.Details.IndexOf(_details);
                logger.LogDebug($"Details: {details}");
                if (details == 4)
                {
                    playerToDie.KillPlayer(new Vector3(), false, causeOfDeath);
                    return;
                }

                playerToDie.KillPlayer(new Vector3(), true, causeOfDeath, details);
            }
        }
    }

    [HarmonyPatch]
    public class NetworkObjectManager
    {
        static GameObject networkPrefab;
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            if (networkPrefab != null)
                return;

            networkPrefab = (GameObject)DeathNoteBase.DNAssetBundle.LoadAsset("Assets/DeathNote/NetworkHandlerDN.prefab");
            networkPrefab.AddComponent<NetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
