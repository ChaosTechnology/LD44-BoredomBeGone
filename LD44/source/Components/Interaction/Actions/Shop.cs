namespace LD44.Components.Interaction.Actions
{
    public class Shop : NPCInteraction
    {
        protected override void StartInteraction()
        {
            base.StartInteraction();
            Menu.Shop shop = new Menu.Shop(scene.game);
            shop.ShowDialog(scene, player, () => {
                shop.Dispose();
                interactPoint.Conclude();
                });
        }
    }
}
