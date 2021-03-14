using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{
    [DefaultExecutionOrder(-28000)]
    public class ModelManager : MonoBehaviour, IModelManager
    {
        [SerializeField]
        private Localization.Language[] m_allLanguages;

        public IModel[] Models { get; private set; }

        private void OnValidate()
        {
            m_allLanguages = Localization.Language.AllLanguages.ToArray();
        }

        public T GetModel<T>() where T : IModel
        {
            foreach(var model in Models)
            {
                if(model is T tModel)
                {
                    return tModel;
                }
            }
            return default;
        }

        private void Awake()
        {
            Models = new IModel[] { 
                new UsersModel(), 
                new SettingsModel(), 
                new ProceduresModel(Path.Combine(Application.persistentDataPath, "Statistics")),
            };
        }
    }
}
