using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Progression
{
    /// <summary>One reusable Eco per player. Consumed before notifying wallet listeners.</summary>
    public class DeathMarker : MonoBehaviour
    {
        [SerializeField, Min(0)] private float recoveryRadius = .65f;
        private EtherWallet owner;
        private HealthSystem health;
        public float Amount { get; private set; }
        public bool IsAvailable { get; private set; }
        public event System.Action<float> OnRecovered;

        public void Place(EtherWallet wallet, Vector3 position, float amount)
        {
            owner = wallet;
            health = wallet.GetComponent<HealthSystem>();
            transform.position = position;
            Amount = amount;
            IsAvailable = amount > 0;
            gameObject.SetActive(IsAvailable);
        }

        public void Clear()
        {
            IsAvailable = false;
            Amount = 0;
            gameObject.SetActive(false);
        }

        private void Update() => TryRecover();

        public bool TryRecover()
        {
            if (!IsAvailable || owner == null || health == null || health.IsDead
                || Vector2.Distance(owner.transform.position, transform.position) > recoveryRadius) return false;
            float recovered = Amount;
            Clear();
            owner.RestoreFromEco(recovered);
            OnRecovered?.Invoke(recovered);
            return true;
        }
    }
}
