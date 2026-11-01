namespace SpaceInvaders.Gameplay
{
    // Value type: event payload published on the EventBus; no allocation per publish.
    public readonly struct InvaderMarched
    {
        public readonly int StepIndex;

        public InvaderMarched(int stepIndex)
        {
            StepIndex = stepIndex;
        }
    }
}
