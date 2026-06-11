namespace AF.Stats
{
    public static class DamageResolver
    {
        public static DamageResult Resolve(ResourcePool pool, DamageRequest request)
        {
            if (pool == null)
            {
                return DamageResult.None;
            }

            return pool.ApplyDamage(request.Amount);
        }
    }
}