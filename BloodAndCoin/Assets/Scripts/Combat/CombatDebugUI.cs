using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BloodAndCoin.Combat
{
    // Bare-bones text readout for manually verifying the initiative queue/turn flow — not
    // meant to survive past the prototype.
    public class CombatDebugUI : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private Text currentTurnText;
        [SerializeField] private Text queueText;
        [SerializeField] private Text battleEndedText;

        private void OnEnable()
        {
            // Legacy Text ships with placeholder content ("New Text") in the Editor — clear
            // it so nothing shows before the battle actually produces real text.
            if (currentTurnText != null) currentTurnText.text = string.Empty;
            if (queueText != null) queueText.text = string.Empty;
            if (battleEndedText != null) battleEndedText.text = string.Empty;

            if (combatManager == null)
                return;

            combatManager.TurnStarted += OnTurnStarted;
            combatManager.RoundStarted += OnRoundStarted;
            combatManager.BattleEnded += OnBattleEnded;
        }

        private void OnDisable()
        {
            if (combatManager == null)
                return;

            combatManager.TurnStarted -= OnTurnStarted;
            combatManager.RoundStarted -= OnRoundStarted;
            combatManager.BattleEnded -= OnBattleEnded;
        }

        private void OnRoundStarted() => RefreshQueue();

        private void OnTurnStarted(CombatUnit unit)
        {
            if (currentTurnText != null)
                currentTurnText.text = $"Ход: {unit.Name} ({unit.Team}) — AP {unit.CurrentActionPoints}, HP {unit.CurrentHp}";

            RefreshQueue();
        }

        private void OnBattleEnded(Team winner)
        {
            if (battleEndedText != null)
                battleEndedText.text = $"Бой окончен. Победила сторона: {winner}";
        }

        private void RefreshQueue()
        {
            if (queueText == null)
                return;

            queueText.text = string.Join("\n", combatManager.InitiativeQueueSnapshot
                .Select(u => $"{u.Name} ({u.Team}) — Иниц. {u.Stats.initiative}, HP {u.CurrentHp}"));
        }
    }
}
