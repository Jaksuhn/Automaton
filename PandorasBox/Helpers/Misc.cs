using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;

namespace Automaton.Helpers
{
    public static class Misc
    {
        public static ExcelSheet<Lumina.Excel.GeneratedSheets.Action> Action = null!;
        public static ExcelSheet<AozAction> AozAction = null!;
        public static ExcelSheet<AozActionTransient> AozActionTransient = null!;

        public static uint AozToNormal(uint id)
        {
            if (id == 0) return 0;
            return AozAction.GetRow(id)!.Action.Row;
        }

        public static uint NormalToAoz(uint id)
        {
            foreach (var aozAction in AozAction)
            {
                if (aozAction.Action.Row == id) return aozAction.RowId;
            }

            throw new Exception("https://tenor.com/view/8032213");
        }
    }
}
