namespace FoodFlow.Contracts;

/// <summary>
/// Deterministic demo identifiers so failure paths can be exercised without a real payment or warehouse API.
/// </summary>
public static class DemoScenarios
{
    public static readonly Guid InStockProductId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid OutOfStockProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TransientFailureProductId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PermanentFailureProductId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid PaymentFailCustomerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DefaultRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid DefaultCustomerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
}
