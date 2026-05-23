namespace LD44.Components.Characters.Brains
{
    class Boss : Enemy
    {
        public override void TakeControl()
        {
            base.TakeControl();
            aggroRangeSq = deaggroRangeSq = float.MaxValue;
        }
    }
}
