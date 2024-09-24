using MCGalaxy.Eco;

namespace MCGalaxy
{
    public sealed class ExampleStoreItem : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.5"; } }
        public override string name { get { return "ExampleStoreItem"; } }

        /// <summary>
        /// Loads the plugin.
        /// </summary>

        public override void Load(bool startup)
        {
            // Add store item(s)
            ExampleItem example = new ExampleItem();
            Economy.Items.Add(example);
            Item item = Economy.GetItem("Example");
            item.Enabled = true;
        }

        /// <summary>
        /// Unloads the plugin.
        /// </summary>

        public override void Unload(bool shutdown)
        {
            // Remove item(s) from store
            Item item = Economy.GetItem("Example");
            item.Enabled = false;
        }
    }

    public sealed class ExampleItem : SimpleItem
    {
        public ExampleItem()
        {
            Aliases = new string[] { "alias1", "alias2", "alias3", "etc" }; // Aliases for store items. E.g, queuelevel and queuelvl would do the same thing
            Price = 50; // The cost of the item in server currency
        }

        public override string Name { get { return "Example"; } } // The name of the store item under /store

        protected override void OnPurchase(Player p, string input)
        {
            string[] args = input.SplitSpaces();

            // OPTIONAL: Add checking here to see if input is valid or remove these comments completely
            // E.g, for something like queuelvl, check if level is a valid level before carrying out purchase

            if (!CheckPrice(p)) return; // Check to see if player can afford the purchase

            // This is where you would add your own features upon purchase
            // For example purposes, I will just send a message to the player whenever they purchase the item
            p.Message("%SYou just wasted %b" + Price + " %Son an example item.");

            // OPTIONAL: Execute a command on purchase
            // UseCommand(p, "CommandName", "optional arguments");

            Economy.MakePurchase(p, Price, "Example"); // Make the purchase
        }

        protected override void OnStoreCommand(Player p)
        {
            p.Message("%T/Buy {0} [description]", Name);
            OutputItemInfo(p);
            p.Message("[long description]");
        }
    }
}
