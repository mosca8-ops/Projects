using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class BaseConditionController : ProcedureObjectController<BaseCondition>
    {
        public string Description => Model.GetDescription();
        

        public BaseConditionController(BaseCondition model, GraphObjectController owner) : base(owner.ViewController, model)
        {
            model.OnModified -= ModelModified;
            model.OnModified += ModelModified;
        }

        protected override void ModelChanged(Object obj)
        {
            
        }

        protected void ModelModified(ProcedureObject model)
        {
            if (model == Model)
            {
                NotifyChange(ModelHasChanges);
            }
        }
    }
}
