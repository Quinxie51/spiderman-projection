namespace SpiderProjection.Runtime
{
    public static class StatePriority
    {
        public static GameplayState Resolve(GameplayState current, GameplayState requested)
        {
            return GetPriority(requested) >= GetPriority(current) ? requested : current;
        }

        public static int GetPriority(GameplayState state)
        {
            switch (state)
            {
                case GameplayState.Defeat:
                    return 100;
                case GameplayState.Hurt:
                    return 90;
                case GameplayState.LedgeClimb:
                case GameplayState.LedgeGrab:
                    return 70;
                case GameplayState.WallCling:
                case GameplayState.WallCrawl:
                    return 60;
                case GameplayState.WebShoot:
                case GameplayState.SwingAttach:
                case GameplayState.SwingLoop:
                case GameplayState.SwingRelease:
                    return 50;
                case GameplayState.Roll:
                case GameplayState.Skid:
                    return 40;
                default:
                    return 10;
            }
        }
    }
}
