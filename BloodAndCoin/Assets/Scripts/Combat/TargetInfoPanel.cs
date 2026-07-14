using UnityEngine;
using UnityEngine.UI;

namespace BloodAndCoin.Combat
{
    // Quick "what happens if I attack" readout — PlayerInputController shows this on the
    // first click on an in-range enemy, and only attacks on a second click on the same
    // target. Purely informational: CalculateHitChance has no side effects and doesn't touch
    // any random roll, so showing it never affects the eventual attack.
    [RequireComponent(typeof(RectTransform))]
    public class TargetInfoPanel : MonoBehaviour
    {
        [SerializeField] private Text infoText;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 70f);

        private RectTransform _panelRect;

        private void Awake()
        {
            _panelRect = GetComponent<RectTransform>();
            gameObject.SetActive(false);
        }

        public void Show(CombatUnit attacker, CombatUnit defender, Vector3 targetWorldPosition, Camera worldCamera)
        {
            bool ranged = attacker.Stats.isRanged;
            int hitChance = AttackResolver.CalculateHitChance(attacker, defender, ranged);

            string armorLine = defender.Stats.maxArmor > 0
                ? $"Броня: {defender.CurrentArmor}/{defender.Stats.maxArmor}\n"
                : string.Empty;

            if (infoText != null)
            {
                infoText.text =
                    $"{defender.Name} ({defender.Team})\n" +
                    $"HP: {defender.CurrentHp}/{defender.Stats.maxHp}\n" +
                    armorLine +
                    $"ОД: {defender.CurrentActionPoints}/{defender.Stats.actionPoints}\n" +
                    $"Шанс попадания: {hitChance}%\n" +
                    "Клик ещё раз — атаковать";
            }

            gameObject.SetActive(true);
            PositionNear(targetWorldPosition, worldCamera);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // Assumes a Screen Space - Overlay canvas (Unity's default Render Mode when a Canvas
        // is created via the UI menu) — under Overlay, a UI element's transform.position IS
        // screen-pixel space already, so no RectTransformUtility conversion is needed. If the
        // canvas ever switches to Screen Space - Camera or World Space, this needs revisiting.
        private void PositionNear(Vector3 worldPosition, Camera worldCamera)
        {
            if (worldCamera == null)
                return;

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            _panelRect.position = screenPoint + (Vector3)screenOffset;
        }
    }
}
