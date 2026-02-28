using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [RequireComponent(typeof(InteractiveObject), typeof(Placeable))]
    public class Bed : MonoBehaviour
    {
        [Header("Sleep Time Settings")]
        [Range(0, 23)]
        [SerializeField] private int sleepStartHour = 20;

        [Range(0, 23)]
        [SerializeField] private int sleepEndHour = 6;

        [Header("Sleep Delay")]
        [SerializeField] private float interactionDelay = 2f;

        [Header("Sleep Restore Effects")]
        [SerializeField] private List<ConsumableEffect> sleepEffects = new();

        [Header("World Control")]
        [SerializeField] private bool freezeAnimalsDuringSleep = true;

        private bool isSleeping;

        public void TrySleep()
        {
            // ✅ MUST RUN COROUTINE, not instant SetTime
            if (!CanSleepExternally())
                return;

            if (isSleeping)
                return;

            StartCoroutine(SleepRoutine());
        }

        private bool CanSleepNow()
        {
            if (TimeClock.Instance == null)
                return false;

            int hour = TimeClock.Instance.Hour;

            // same-day window (e.g. 8 -> 18)
            if (sleepStartHour < sleepEndHour)
                return hour >= sleepStartHour && hour < sleepEndHour;

            // overnight window (e.g. 20 -> 6)
            return hour >= sleepStartHour || hour < sleepEndHour;
        }

        private IEnumerator SleepRoutine()
        {
            Debug.Log("Sleep routine start");

            isSleeping = true;

            GameManager.instance.SetCanPause(false);
            Character.instance.DisableMovements();
            Character.instance.SetCharacterBusy(true);

            // ✅ THIS is your sleep delay
            yield return new WaitForSeconds(interactionDelay);

            bool froze = false;
            if (freezeAnimalsDuringSleep)
            {
                Animal.GlobalFreeze = true;
                froze = true;
            }

            int hoursSlept = CalculateHoursSlept();
            ApplySleepEffects(hoursSlept);

            // ✅ Advance time (day + hour) correctly
            AdvanceToMorning();

            // ✅ Force lighting refresh AFTER SetTime
            if (TimeClock.Instance != null)
                TimeClock.Instance.ForceRefreshVisual();

            yield return new WaitForSeconds(0.5f);

            if (froze)
                Animal.GlobalFreeze = false;

            Character.instance.SetCharacterBusy(false);
            Character.instance.EnableMovements();
            GameManager.instance.SetCanPause(true);

            isSleeping = false;

            Debug.Log("Sleep routine end");
        }

        private int CalculateHoursSlept()
        {
            if (TimeClock.Instance == null) return 0;

            int currentHour = TimeClock.Instance.Hour;
            int wakeHour = sleepEndHour;

            if (sleepStartHour < sleepEndHour)
                return Mathf.Max(0, wakeHour - currentHour);

            if (currentHour >= sleepStartHour)
                return (24 - currentHour) + wakeHour;

            return Mathf.Max(0, wakeHour - currentHour);
        }

        private void ApplySleepEffects(int hoursSlept)
        {
            var attributeSystem = Character.instance.characterAttribute;
            if (attributeSystem == null) return;

            foreach (var effect in sleepEffects)
            {
                if (effect == null) continue;

                switch (effect.effectType)
                {
                    case ConsumableEffectType.Instant:
                        {
                            float total = effect.instantAmount * hoursSlept;
                            attributeSystem.AddCurrentAttributeValue(effect.attributeType, total);
                            break;
                        }

                    case ConsumableEffectType.OverTime:
                        {
                            float ticks = Mathf.Floor(effect.duration / effect.tickInterval);
                            float total = effect.amountPerTick * ticks;
                            attributeSystem.AddCurrentAttributeValue(effect.attributeType, total);
                            break;
                        }

                    case ConsumableEffectType.DurationBuff:
                        {
                            // keep your existing behavior
                            Character.instance.StartCoroutine(Character.instance.ApplyOverTime(effect));
                            break;
                        }
                }
            }
        }

        private void AdvanceToMorning()
        {
            if (TimeClock.Instance == null) return;

            int currentDay = TimeClock.Instance.Day;
            int currentHour = TimeClock.Instance.Hour;
            int wakeHour = sleepEndHour;

            // ✅ If wake hour is "tomorrow" relative to current hour → day + 1
            // Example: current 22, wake 6 => next day
            // Example: current 2, wake 6 => same day
            if (wakeHour <= currentHour)
                currentDay++;

            TimeClock.Instance.SetTime(0, wakeHour, currentDay);
        }

        public bool CanSleepExternally()
        {
            return !isSleeping && CanSleepNow();
        }
    }
}