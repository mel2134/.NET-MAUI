using MonkeyFinder.Services;
namespace MonkeyFinder.ViewModel;

public partial class MonkeysViewModel : BaseViewModel
{
    MonkeyService monkeyService;
    public ObservableCollection<Monkey> Monkeys { get; } = new();
    //public Command GetMonkeysCommand { get; }
    IConnectivity connectivity;
    IGeolocation geolocation;
    public MonkeysViewModel(MonkeyService monkeyService, IConnectivity connectivity, IGeolocation geolocation)
    {
        Title = "Monkey Finder";
        this.monkeyService = monkeyService;
        this.connectivity = connectivity;
        this.geolocation = geolocation;

        //GetMonkeysCommand = new Command(async () => await GetMonkeysAsync());

    }
    [ObservableProperty]
    bool isRefreshing;
    [RelayCommand]
    async Task GetClosestMonkeyAsync()
    {
        if (IsBusy || Monkeys.Count == 0) return;
        try
        {
            var loc = await geolocation.GetLastKnownLocationAsync();
            if (loc is null) loc = await geolocation.GetLocationAsync(new GeolocationRequest{ DesiredAccuracy= GeolocationAccuracy.Medium,Timeout=TimeSpan.FromSeconds(30)});
            
            if (loc is null) return;

            var first = Monkeys.OrderBy(m => loc.CalculateDistance(m.Latitude, m.Longitude, DistanceUnits.Miles)).FirstOrDefault();
            
            if (first is null) return;

            await Shell.Current.DisplayAlert("Closest monkey",$"{first.Name} in {first.Location}","OK!");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("Error!", $"Unable to get closest monkey: {ex}", "OK");
        }
    }
    [RelayCommand]
    async Task GoToDetailsAsync(Monkey monkey)
    {
        if (monkey is null) return;
        //await Shell.Current.GoToAsync($"{nameof(DetailsPage)}?id={monkey.Name}");
        await Shell.Current.GoToAsync($"{nameof(DetailsPage)}",true,new Dictionary<string, object>
        {
            {"Monkey",monkey}
        });
    }

    [RelayCommand]
    async Task GetMonkeysAsync()
    {
        if (IsBusy) return;
        try
        {
            if (connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("No Internet", $"No internet!", "OK");
                return;
            }
            IsBusy = true;
            var monkeys = await monkeyService.GetMonkeys();
            if (Monkeys.Count != 0) Monkeys.Clear();
            foreach (var monkey in monkeys)
            {
                Monkeys.Add(monkey);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("Error!",$"Unable to get monkeys: {ex}","OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
