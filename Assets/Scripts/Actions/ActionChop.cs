// using System.Collections.Generic;
// using UnityEngine;

// namespace Yamigisa
// {
//     [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Chop", order = 50)]
//     public class ActionChop : ActionBase
//     {
//         public List<GroupData> choppableItems;

//         public override void DoAction(Character character, Component context)
//         {
//             if (CanDoAction(Inventory.Instance.GetSelectedQuickItemData()))
//             {
//                 Selectable selectable = context as Selectable;
//                 ItemData item = selectable.ItemData;

//                 if (choppableItems.Contains(item.groupData))
//                 {
//                     Inventory.Instance.AddItem(item);
//                 }
//             }
//         }
//     }
// }