using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Player.Views;
using UnityEngine;
using UnityEngine.TestTools;

namespace TXT.WEAVR.Player.Controller
{
    public class SettingsControllerTest
    {
        public IDataProvider Provider { get; private set; }
        public SettingsController Controller { get; private set; }

        [SetUp]
        private void Setup()
        {
            Provider = Substitute.For<IDataProvider>();
            IModelManager modelManager = Substitute.For<IModelManager>();
            ISettingsModel settingsModel = Substitute.For<ISettingsModel>();
            IUserModel userModel = Substitute.For<IUserModel>();
            ISettingsView settingsView = Substitute.For<ISettingsView>();
            IViewManager viewManager = Substitute.For<IViewManager>();

            // Wire the mocks together
            Controller = new SettingsController(Provider);

            Provider.ViewManager.Returns(viewManager);
            //Provider.ModelManager.Returns(modelManager);
            
            modelManager.GetModel<ISettingsModel>().Returns(settingsModel);
            modelManager.GetModel<IUserModel>().Returns(userModel);

            viewManager.GetView<ISettingsView>().Returns(settingsView);

            // Some basic logic for components
            
        }

        // A Test behaves as an ordinary method
        [Test]
        public void SubsequentShowView_Pass()
        {
            List<(string, object)> settings = new List<(string, object)>();
            Provider.ViewManager.GetView<ISettingsView>().When(v => v.ClearSettings()).Do(c => settings.Clear());
            // TODO: Finish this test

        }
    }
}
