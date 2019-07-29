using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FModel
{
    public struct SchematicInfoEntry : IEquatable<SchematicInfoEntry>
    {
        internal SchematicInfoEntry(string myIngredientItemDefinition, string myIngredientQuantity)
        {
            theIngredientItemDefinition = myIngredientItemDefinition;
            theIngredientQuantity = myIngredientQuantity;
        }
        public string theIngredientItemDefinition { get; set; }
        public string theIngredientQuantity { get; set; }

        bool IEquatable<SchematicInfoEntry>.Equals(SchematicInfoEntry other)
        {
            throw new NotImplementedException();
        }
    }
}
