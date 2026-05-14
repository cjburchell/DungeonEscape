using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class HealerViewModel
    {
        public HealerFocus Focus { get; private set; }
        public int SelectedServiceIndex { get; private set; }
        public int SelectedTargetIndex { get; private set; }

        public void Reset()
        {
            Focus = HealerFocus.Service;
            SelectedServiceIndex = 0;
            SelectedTargetIndex = 0;
        }

        public string GetHealerName(TiledObjectInfo healerObject)
        {
            return healerObject == null || string.IsNullOrEmpty(healerObject.Name) ? "Healer" : healerObject.Name;
        }

        public string GetHealerText(TiledObjectInfo healerObject)
        {
            string text;
            return healerObject != null &&
                   healerObject.Properties != null &&
                   healerObject.Properties.TryGetValue("Text", out text) &&
                   !string.IsNullOrEmpty(text)
                ? text
                : "Do you require my services as a healer?";
        }

        public int GetHealerCost(TiledObjectInfo healerObject)
        {
            string value;
            int result;
            return healerObject != null &&
                   healerObject.Properties != null &&
                   healerObject.Properties.TryGetValue("Cost", out value) &&
                   int.TryParse(value, out result)
                ? result
                : 25;
        }

        public List<HealerServiceRow> BuildServices(Party party, TiledObjectInfo healerObject)
        {
            if (party == null)
            {
                return new List<HealerServiceRow>();
            }

            var cost = GetHealerCost(healerObject);
            var wounded = party.AliveMembers.Where(member => member.Health != member.MaxHealth).ToList();
            var magicMissing = party.AliveMembers.Where(member => member.Magic != member.MaxMagic).ToList();
            var statusMembers = party.AliveMembers.Where(member => member.Status != null && member.Status.Count != 0).ToList();
            var dead = party.DeadMembers.ToList();
            var rows = new List<HealerServiceRow>();
            if (wounded.Count > 0)
            {
                rows.Add(new HealerServiceRow { Service = HealerService.Heal, Label = "Heal", Cost = cost, Targets = wounded, NeedsTarget = true });
                if (wounded.Count > 1)
                {
                    rows.Add(new HealerServiceRow { Service = HealerService.HealAll, Label = "Heal All", Cost = cost * wounded.Count });
                }
            }

            if (magicMissing.Count > 0)
            {
                rows.Add(new HealerServiceRow { Service = HealerService.RenewMagic, Label = "Renew Magic", Cost = cost * 2 * magicMissing.Count });
            }

            if (statusMembers.Count > 0)
            {
                rows.Add(new HealerServiceRow { Service = HealerService.Cure, Label = "Cure", Cost = cost * 2, Targets = statusMembers, NeedsTarget = true });
            }

            if (dead.Count > 0)
            {
                rows.Add(new HealerServiceRow { Service = HealerService.Revive, Label = "Revive", Cost = cost * 10, Targets = dead, NeedsTarget = true });
            }

            return rows;
        }

        public void ClampServiceSelection(IList<HealerServiceRow> services)
        {
            SelectedServiceIndex = Clamp(SelectedServiceIndex, 0, Math.Max((services == null ? 0 : services.Count) - 1, 0));
        }

        public void ClampTargetSelection(HealerServiceRow service)
        {
            var count = service == null || service.Targets == null ? 0 : service.Targets.Count;
            SelectedTargetIndex = Clamp(SelectedTargetIndex, 0, Math.Max(count - 1, 0));
        }

        public bool SetFocus(HealerFocus focus)
        {
            if (Focus == focus)
            {
                return false;
            }

            Focus = focus;
            return true;
        }

        public bool SetSelectedServiceIndex(int index)
        {
            if (SelectedServiceIndex == index)
            {
                return false;
            }

            SelectedServiceIndex = index;
            SelectedTargetIndex = 0;
            return true;
        }

        public bool SetSelectedTargetIndex(int index)
        {
            if (SelectedTargetIndex == index)
            {
                return false;
            }

            SelectedTargetIndex = index;
            return true;
        }

        public HealerServiceRow GetSelectedService(IList<HealerServiceRow> services)
        {
            if (services == null || services.Count == 0)
            {
                return null;
            }

            ClampServiceSelection(services);
            return services[SelectedServiceIndex];
        }

        public Hero GetSelectedTarget(HealerServiceRow service)
        {
            if (service == null || service.Targets == null || service.Targets.Count == 0)
            {
                return null;
            }

            ClampTargetSelection(service);
            return service.Targets[SelectedTargetIndex];
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
