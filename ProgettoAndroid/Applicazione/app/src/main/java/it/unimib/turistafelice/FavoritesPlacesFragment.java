package it.unimib.turistafelice;

import android.content.SharedPreferences;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.activity.OnBackPressedCallback;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.lifecycle.Observer;
import androidx.lifecycle.ViewModelProvider;
import androidx.navigation.NavController;
import androidx.navigation.Navigation;
import androidx.recyclerview.widget.LinearLayoutManager;

import com.google.firebase.auth.FirebaseAuth;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;

import it.unimib.turistafelice.adapter.PlaceAdapter;
import it.unimib.turistafelice.databinding.FragmentFavoritesPlacesBinding;
import it.unimib.turistafelice.databinding.FragmentNoFavoritesBinding;
import it.unimib.turistafelice.model.Place;
import it.unimib.turistafelice.model.Resource;
import it.unimib.turistafelice.viewmodel.PlaceViewModel;


public class FavoritesPlacesFragment extends Fragment {

    private static final String TAG="FavoritesPlacesFragment";
    private FragmentFavoritesPlacesBinding binding;
    private  FragmentNoFavoritesBinding bindingNoFavorites;
    private PlaceViewModel placeViewModel;
    private PlaceAdapter PlaceAdapter;
    private FirebaseAuth fAuth;

    private int totalItemCount;
    private int lastVisibleItem;
    private int visibleItemCount;


    public FavoritesPlacesFragment() {
        // Required empty public constructor
    }

    public static FavoritesPlacesFragment newInstance() { return new FavoritesPlacesFragment();}

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        List<String> queries = getPlaceId(getArguments().getString("selected_city"));
        if(queries != null){
            binding = FragmentFavoritesPlacesBinding.inflate(getLayoutInflater());
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
        placeViewModel = new ViewModelProvider(requireActivity()).get(PlaceViewModel.class);

        List<String> queries = getPlaceId(getArguments().getString("selected_city"));

        if(queries != null){
            LinearLayoutManager layoutManager = new LinearLayoutManager(getActivity());
            binding.placeFavoriteResultRecyclerView.setLayoutManager(layoutManager);

            PlaceAdapter placeAdapter = new PlaceAdapter(getPlacesList(queries), getArguments().getString("selected_city").toUpperCase(),  getActivity(), new PlaceAdapter.OnItemClickListener() {
                @Override
                public void onItemClick(Place place) {

                    FavoritesPlacesFragmentDirections.FavoritePlaceDetailsAction favoritePlaceDetailsAction =
                            FavoritesPlacesFragmentDirections.favoritePlaceDetailsAction(place,
                                    getArguments().getString("selected_city").trim());
                    Navigation.findNavController(view).navigate(favoritePlaceDetailsAction);
                }

            });
            binding.placeFavoriteResultRecyclerView.setAdapter(placeAdapter);

            final Observer<Resource<List<Place>>> observer = new Observer<Resource<List<Place>>>() {
                @Override
                public void onChanged(Resource<List<Place>> placesResource) {
                    Log.d(TAG, "onChanged: " + placesResource.getData());
                    placeAdapter.setData(placesResource.getData());
                    if (placesResource.getData() != null) {
                        Log.d(TAG, "Success - Total results: " + placesResource.getData().size() + " Status code: " +
                                placesResource.getStatus());

                        for (int i = 0; i < placesResource.getData().size(); i++) {
                            Log.d(TAG, "Place: " + placesResource.getData().get(i).getName());
                        }
                    } else {
                        Log.d(TAG, "Error - Status code: " + placesResource.getStatus());
                    }
                }
            };

            String linguaggio = "";

            if (Locale.getDefault().getLanguage().equals("it")) {
                linguaggio = "it";
            }
            else if (Locale.getDefault().getLanguage().equals("en")) {
                linguaggio = "en";
            }
            placeViewModel.getFavoritePlaces(queries, linguaggio).observe(getViewLifecycleOwner(), observer);


            OnBackPressedCallback callback = new OnBackPressedCallback(
                    true // default to enabled
            ) {
                @Override
                public void handleOnBackPressed() {
                    placeViewModel.deletePlaces();
                    NavController controller = Navigation.findNavController(view);
                    controller.navigateUp();
                }
            };
            requireActivity().getOnBackPressedDispatcher().addCallback(
                    getViewLifecycleOwner(), // LifecycleOwner
                    callback);
        }
    }

    private List<Place> getPlacesList(List<String> queries) {
        String linguaggio = "";

        if (Locale.getDefault().getLanguage().equals("it")) {
            linguaggio = "it";
        }
        else if (Locale.getDefault().getLanguage().equals("en")) {
            linguaggio = "en";
        }

        Resource<List<Place>> placeListResource = placeViewModel.getFavoritePlaces(queries, linguaggio).getValue();

        if (placeListResource != null) {
            return placeListResource.getData();
        }

        return null;
    }

    private List<String> getPlaceId(String city) {
        fAuth = FirebaseAuth.getInstance();
        Set<String> placesId = new HashSet<>();
        String userId = fAuth.getCurrentUser().getUid();
        SharedPreferences sharedPreferences = getActivity().getSharedPreferences(userId, getContext().MODE_PRIVATE);
        placesId = sharedPreferences.getStringSet(city, null);
        Log.d(TAG, "getTripList: "+ placesId);
        ArrayList<String> placesIdByCities = new ArrayList<>();
        if(placesId != null){
            placesIdByCities.addAll(placesId);
            return placesIdByCities;
        } else {
            return null;
        }
    }
}
