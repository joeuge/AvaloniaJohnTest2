﻿namespace Caliburn.Micro
{
    public class AvaloniaNavigation : INavigationService
    {
        public Task GoBackAsync(bool animated = true)
        {
            throw new NotImplementedException();
        }

        public Task GoBackToRootAsync(bool animated = true)
        {
            throw new NotImplementedException();
        }

        public Task NavigateToViewAsync(Type viewType, object parameter = null, bool animated = true)
        {
            throw new NotImplementedException();
        }

        public Task NavigateToViewAsync<T>(object parameter = null, bool animated = true)
        {
            throw new NotImplementedException();
        }

        public Task NavigateToViewModelAsync(Type viewModelType, object parameter = null, bool animated = true)
        {
            throw new NotImplementedException();
        }

        public Task NavigateToViewModelAsync<T>(object parameter = null, bool animated = true)
        {
            throw new NotImplementedException();
        }
    }
}
