using ThunderRoad;

namespace TrainingDroid
{
    public class TrainingDroidModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<TrainingDroid>();
        }
    }
}