﻿using SophiAppCE.Common;
using SophiAppCE.Helpers;
using SophiAppCE.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Localizzation = SophiAppCE.Common.Localization;

namespace SophiAppCE.ViewModel
{
    class AppViewModel : INotifyPropertyChanged
    {        
        private RelayCommand selectAllCommand;
        private RelayCommand controlClickedCommand;
        private RelayCommand changePageCommand;
        private RelayCommand applyAllCommand;
        private RelayCommand changeLanguageCommand;

        private Localizzation Localizzation = new Localizzation();
        private UInt16 activeControlsCounter = default(UInt16);
        private string activePage = Tags.Privacy;
        private string nowAppliedText = string.Empty;

        public List<ControlModel> ControlsModelsCollection { get; set; }

        public AppViewModel()
        {            
            ControlsModelsCollectionFilling();            
        }          

        public LanguageFamily CurrentLanguage
        {
            get => Localizzation.Current;            
        }        

        public UInt16 ActiveControlsCounter
        {           
            get => activeControlsCounter;
            private set 
            {
                activeControlsCounter = value;
                OnPropertyChanged("ActiveControlsCounter");
            }
        }

        public string ActivePage
        {
            get => activePage;
            private set
            {
                activePage = value;
                OnPropertyChanged("ActivePage");
            }
        }

        public string NowAppliedText
        {
            get => nowAppliedText;
            private set
            {
                nowAppliedText = value;
                OnPropertyChanged("NowAppliedText");
            }
        }

        private void ChangeActiveControlsCounter(bool value)
        {
            if (value)
                ActiveControlsCounter++;
            else
                ActiveControlsCounter--;
        }

        private void ControlsModelsCollectionFilling()
        {
            IEnumerable<JsonData> jsons = Parser.ParseJson();
            IEnumerable<ControlModel> models = ControlsFabric.CreateAll(jsonData: jsons, language: Localizzation.Current);
            ControlsModelsCollection = new List<ControlModel>(models);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyChanged)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyChanged));
        }

        public RelayCommand ChangeLanguageCommand => changeLanguageCommand ?? new RelayCommand(ChangeLanguage);

        private void ChangeLanguage(object args)
        {
            Localizzation.Change(callBack: ChangeLanguageCallback);
        }

        private void ChangeLanguageCallback()
        {
            ControlsModelsCollection.ForEach(c => c.ChangeLanguageTo(CurrentLanguage));
            OnPropertyChanged("CurrentLanguage");
        }

        public RelayCommand ApplyAllCommand => applyAllCommand ?? new RelayCommand(ApplyAll);        

        public RelayCommand SelectAllCommand => selectAllCommand ?? new RelayCommand(SelectAll);

        public RelayCommand ControlClickedCommand => controlClickedCommand ?? new RelayCommand(ControlClicked);

        public RelayCommand ChangePageCommand => changePageCommand ?? new RelayCommand(ChangePage);

        private async void ApplyAll(object args)
        {
            List<ControlModel> selectedModel = ControlsModelsCollection.Where(m => m.IsChanged == true).ToList();
            await ApplySettingsAsync(selectedModel);                                    
        }

        private async Task ApplySettingsAsync(List<ControlModel> controlsModels)
        {
            await Task.Run(async () =>
            {
                controlsModels.ForEach(m =>
                {
                    NowAppliedText = m.Header;
                    bool state = !(m.ActualState & m.State); // !(true & false) = true (включить) ; !(true & true) = false (выключить)
                    m.Action.Run(state);
                    Thread.Sleep(3000);
                });
            });
        }

        private void ChangePage(object args) => ActivePage = args as string;

        private void ControlClicked(object args)
        {
            UInt16 id = Convert.ToUInt16(args);
            ControlModel controlModel = ControlsModelsCollection.Where(m => m.Id == id).First();
            controlModel.ChangeState();            
            ChangeActiveControlsCounter(controlModel.ActualState);            
        }

        private void SelectAll(object args)
        {
            object[] arg = args as object[];
            string tag = arg.First() as string;
            bool state = Convert.ToBoolean(arg.Last());
            ControlsModelsCollection.Where(m => m.Tag == tag && m.State != true && m.ActualState != state)
                                    .ToList()
                                    .ForEach(m =>
                                    {
                                        m.ChangeState();
                                        ChangeActiveControlsCounter(m.ActualState);
                                    });                                             
        }
    }
}