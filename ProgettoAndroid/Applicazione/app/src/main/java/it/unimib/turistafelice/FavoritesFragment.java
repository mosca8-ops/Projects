package it.unimib.turistafelice;

import android.content.SharedPreferences;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.lifecycle.Observer;
import androidx.lifecycle.ViewModelProvider;
import androidx.navigation.Navigation;
import androidx.recyclerview.widget.LinearLayoutManager;

import com.google.firebase.auth.FirebaseAuth;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import it.unimib.turistafelice.adapter.TripAdapter;
import it.unimib.turistafelice.databinding.FragmentFavoritesFragmentBinding;
import it.unimib.turistafelice.databinding.FragmentNoFavoritesBinding;
import it.unimib.turistafelice.model.Resource;
import it.unimib.turistafelice.model.Trip;
import it.unimib.turistafelice.utils.Constants;
import it.unimib.turistafelice.viewmodel.UnsplashViewModel;

public class FavoritesFragment extends Fragment {


    FragmentFavoritesFragmentBinding binding;
    FragmentNoFavoritesBinding bindingNoFavorites;
    private static final String TAG="FavoritesFragment";
    private FirebaseAuth fAuth;
    private UnsplashViewModel unsplashViewModel;

    public FavoritesFragment() {
        // Required empty public constructor
    }

    public static FavoritesFragment newInstance() {
        FavoritesFragment fragment = new FavoritesFragment();
        return fragment;
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        List<String> cityNames;
        cityNames = getCityNames();
        if(cityNames.size()!=0){
            binding = FragmentFavoritesFragmentBinding.inflate(getLayoutInflater());
            Log.d(TAG, "onCreateView: caricata recycler");
            Log.d(TAG, "onCreateView: " + cityNames);
            return binding.getRoot();
        } else {
            bindingNoFavorites = FragmentNoFavoritesBinding.inflate(getLayoutInflater());
            Log.d(TAG, "onCreateView: caricata noInterest");
            return bindingNoFavorites.getRoot();
        }
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        List<String> cityNames = new ArrayList<>();

        Map<String, String> urlCityPhoto = new HashMap<>();
        List<Trip> trips = new ArrayList<>();
        cityNames = getCityNames();

        if (cityNames.size() != 0) {
            LinearLayoutManager layoutManager = new LinearLayoutManager(getActivity());
            binding.tripRecyclerView.setLayoutManager(layoutManager);
            unsplashViewModel = new ViewModelProvider(requireActivity()).get(UnsplashViewModel.class);


            trips = getTripList(cityNames);
            Log.d(TAG, "names: " + cityNames);


            TripAdapter tripAdapter = new TripAdapter(trips, getActivity(), new TripAdapter.OnItemClickListener() {
                @Override
                public void onItemClick(Trip cityNameTrip) {
                    Log.d(TAG, "onItemClick: " + cityNameTrip.getName());
                    FavoritesFragmentDirections.FavoritePlaceIdAction favoritePlaceIdAction=
                            FavoritesFragmentDirections.favoritePlaceIdAction(cityNameTrip.getName());
                    Navigation.findNavController(view).navigate(favoritePlaceIdAction);
                }
            });
            binding.tripRecyclerView.setAdapter(tripAdapter);

            unsplashViewModel.getUrl(cityNames).observe(getViewLifecycleOwner(), new Observer<Resource<Map<String, String>>>() {
                @Override
                public void onChanged(Resource<Map<String, String>> listResource) {
                    Log.d(TAG, "onChanged: " + listResource.getData());
                    tripAdapter.addUrl(listResource.getData());
                }
            });
        }
    }

    private List<Trip> getTripList(List<String> cityNames) {
        Log.d(TAG, "cityNames: "+cityNames);
        List<Trip> trips = new ArrayList<>();
        for(int i = 0; i<cityNames.size(); i++){
            trips.add(new Trip(cityNames.get(i),null));
        }
        return trips;
    }

    private List<String> getCityNames() {
        fAuth = FirebaseAuth.getInstance();
        Set<String> cityTripNames = new HashSet<>();
        String userId = fAuth.getCurrentUser().getUid();
        SharedPreferences sharedPreferences = getActivity().getSharedPreferences(userId, getContext().MODE_PRIVATE);
        cityTripNames = sharedPreferences.getStringSet(Constants.ALL_TRIPS, null);
        Log.d(TAG, "getTripList: "+ cityTripNames);
        ArrayList<String> userCityTripNames = new ArrayList<>();
        if(cityTripNames != null){
            userCityTripNames.addAll(cityTripNames);
            return userCityTripNames;
        } else {
            return null;
        }
    }
}

