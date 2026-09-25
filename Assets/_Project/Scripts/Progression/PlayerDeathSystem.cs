using UnityEngine;
using Palsoul.Base;
using Palsoul.Combat;
using Palsoul.Creatures;
using Palsoul.Utils;

namespace Palsoul.Progression
{
    [RequireComponent(typeof(PlayerController), typeof(EtherWallet))]
    public class PlayerDeathSystem : MonoBehaviour
    {
        [SerializeField] private DeathMarker markerPrefab;
        [Tooltip("Checkpoint used before the player's first visit.")]
        [SerializeField] private AnchorpointController initialAnchor;
        private PlayerController player;
        private EtherWallet wallet;
        private Vector3 initialPosition;
        public AnchorpointController LastAnchor { get; private set; }
        public DeathMarker Marker { get; private set; }
        public float LastLostEther { get; private set; }
        public bool AwaitingRespawn { get; private set; }
        public event System.Action<float> OnDied;
        public event System.Action OnRespawned;
        public event System.Action<float> OnEcoRecovered;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            wallet = GetComponent<EtherWallet>();
            initialPosition = transform.position;
            LastAnchor = initialAnchor;
            Marker = Instantiate(markerPrefab);
            Marker.Clear();
            Marker.OnRecovered += Recovered;
            player.HealthSystem.OnDeath += Died;
        }

        public void Visit(AnchorpointController anchor)
        {
            if (anchor != null && player != null && !player.HealthSystem.IsDead) LastAnchor = anchor;
        }

        private void Died()
        {
            if (AwaitingRespawn) return;
            AwaitingRespawn = true;
            GetComponent<CaptureSystem>()?.CancelThrow();
            GetComponent<SquadController>()?.SuspendForDeath();
            LastLostEther = wallet.TakeDeath();
            // Replacing the payload also invalidates an unrecovered previous Eco,
            // including when this death carries no Ether.
            Marker.Place(wallet, transform.position, LastLostEther);
            OnDied?.Invoke(LastLostEther);
        }

        public bool Respawn()
        {
            if (!AwaitingRespawn) return false;
            Vector3 position = LastAnchor != null ? LastAnchor.RespawnPosition : initialPosition;
            player.RespawnAt(position);
            GetComponent<SquadController>()?.Respawn();
            Physics2D.SyncTransforms();
            if (Camera.main != null) Camera.main.GetComponent<CameraFollow>()?.SnapToTarget();
            AwaitingRespawn = false;
            OnRespawned?.Invoke();
            return true;
        }

        private void Recovered(float amount) => OnEcoRecovered?.Invoke(amount);
        private void OnDestroy()
        {
            if (player != null) player.HealthSystem.OnDeath -= Died;
            if (Marker != null)
            {
                Marker.OnRecovered -= Recovered;
                Destroy(Marker.gameObject);
            }
        }
    }
}
