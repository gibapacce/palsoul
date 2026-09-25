using UnityEngine;

namespace Palsoul.Utils
{
    /// <summary>
    /// Câmera que segue o player com suavização opcional, projetada para trabalhar
    /// em conjunto com o componente PixelPerfectCamera do Unity 2D.
    ///
    /// Setup na cena:
    ///   1. Adicione este script na Main Camera.
    ///   2. Adicione também o componente PixelPerfectCamera (package 2D Pixel Perfect):
    ///        - Assets Per Unit : 16
    ///        - Reference Resolution X: 320 / Y: 180  (conforme GDD seção 8)
    ///        - Crop Frame: Both
    ///        - Grid Snapping: Upscale Render Texture  (evita sub-pixel jitter)
    ///   3. Arraste o Transform do Player para o campo "Target".
    ///
    /// Por que LateUpdate?
    ///   Garante que a câmera se mova APÓS o player ter sido movido no FixedUpdate/Update,
    ///   evitando um frame de atraso visível.
    ///
    /// Por que snapping inteiro?
    ///   Com pixel-perfect, a câmera precisa estar em posições inteiras de pixel para
    ///   não introduzir shimmer. O PixelPerfectCamera já faz isso internamente, mas
    ///   manter o Transform em pixel-snap aqui é uma camada extra de segurança.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Alvo")]
        [Tooltip("Transform do player que a câmera vai seguir.")]
        [SerializeField] private Transform target;

        [Header("Suavização")]
        [Tooltip("Tempo de suavização em segundos. 0 = sem suavização (câmera travada no player).")]
        [Min(0f)]
        [SerializeField] private float smoothTime = 0.08f;

        [Header("Offset")]
        [Tooltip("Deslocamento em unidades Unity em relação ao alvo (ex.: (0, 0.5) para olhar levemente acima do player).")]
        [SerializeField] private Vector2 offset = Vector2.zero;

        [Header("Limites de Câmera (opcional)")]
        [Tooltip("Ativa restrição de bounds para que a câmera não saia do mapa.")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 boundsMin = new Vector2(-50f, -50f);
        [SerializeField] private Vector2 boundsMax = new Vector2( 50f,  50f);

        // Velocidade interna usada pelo SmoothDamp
        private Vector3 _velocity = Vector3.zero;

        // Z fixo da câmera (não deve mudar em 2D)
        private float _fixedZ;

        private void Awake()
        {
            _fixedZ = transform.position.z;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Posição desejada = posição do alvo + offset
            Vector3 desiredPos = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                _fixedZ
            );

            // Aplica suavização (ou travamento direto se smoothTime == 0)
            Vector3 newPos = smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, smoothTime)
                : desiredPos;

            // Aplica restrição de bounds se ativada
            if (useBounds)
            {
                newPos.x = Mathf.Clamp(newPos.x, boundsMin.x, boundsMax.x);
                newPos.y = Mathf.Clamp(newPos.y, boundsMin.y, boundsMax.y);
            }

            // Mantém Z fixo (câmera 2D não deve se mover no eixo Z)
            newPos.z = _fixedZ;

            transform.position = newPos;
        }

        /// <summary>
        /// Teleporta a câmera instantaneamente para o alvo (sem suavização).
        /// Útil ao carregar uma nova cena para evitar o "slide" de entrada.
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            _velocity = Vector3.zero;
            transform.position = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                _fixedZ
            );
        }

        /// <summary>Define o alvo da câmera em runtime (útil para cutscenes ou troca de personagem).</summary>
        public void SetTarget(Transform newTarget) => target = newTarget;

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;
            // Visualiza os limites da câmera na cena
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Vector3 center = new Vector3((boundsMin.x + boundsMax.x) / 2f,
                                         (boundsMin.y + boundsMax.y) / 2f, 0f);
            Vector3 size   = new Vector3(boundsMax.x - boundsMin.x,
                                         boundsMax.y - boundsMin.y, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
