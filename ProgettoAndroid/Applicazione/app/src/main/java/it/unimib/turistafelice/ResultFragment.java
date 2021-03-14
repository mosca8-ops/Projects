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
import java.util.List;
import java.util.Locale;
import java.util.Set;

import it.unimib.turistafelice.adapter.PlaceAdapter;
import it.unimib.turistafelice.databinding.FragmentResultBinding;
import it.unimib.turistafelice.model.Place;
import it.unimib.turistafelice.model.Resource;
import it.unimib.turistafelice.utils.Constants;
import it.unimib.turistafelice.viewmodel.PlaceViewModel;


public class ResultFragment extends Fragment {

    private static final String TAG="ResultFragment";
    private FragmentResultBinding binding;
    private PlaceViewModel placeViewModel;
    private PlaceAdapter PlaceAdapter;
    private FirebaseAuth fAuth;

    private int totalItemCount;
    private int lastVisibleItem;
    private int visibleItemCount;


    public ResultFragment() {
        // Required empty public constructor
    }

    public static ResultFragment newInstance() { return new ResultFragment();}

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        binding = FragmentResultBinding.inflate(getLayoutInflater());
        return binding.getRoot();
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        placeViewModel = new ViewModelProvider(requireActivity()).get(PlaceViewModel.class);
        ArrayList<String> queries = new ArrayList<>();
        for(String interest : getInterests()) {
            queries.add(getArguments().getString("city_name_search").trim() + interest);
        }

        LinearLayoutManager layoutManager = new LinearLayoutManager(getActivity());
        binding.placeResultRecyclerView.setLayoutManager(layoutManager);
        PlaceAdapter placeAdapter = new PlaceAdapter(getPlacesList(queries), getArguments().getString("city_name_search").trim().toUpperCase(), getActivity(), new PlaceAdapter.OnItemClickListener() {
            @Override
            public void onItemClick(Place place) {
                Log.d(TAG, getArguments().getString("city_name_search").trim());
                ResultFragmentDirections.DetailsPlaceAction detailsPlaceAction =
                        ResultFragmentDirections.detailsPlaceAction(place, getArguments().getString("city_name_search").trim());
                Navigation.findNavController(view).navigate(detailsPlaceAction);
            }

        });

        binding.placeResultRecyclerView.setAdapter(placeAdapter);


        final Observer<Resource<List<Place>>> observer = new Observer<Resource<List<Place>>>() {
            @Override
            public void onChanged(Resource<List<Place>> placesResource) {

                placeAdapter.setData(placesResource.getData());
                if (placesResource.getData() != null) {
                    Log.d(TAG, "Success - Total results: " + placesResource.getData().size() + " Status code: " +
                            placesResource.getStatus());
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
        placeViewModel.getPlaces(queries, linguaggio).observe(getViewLifecycleOwner(), observer);

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

    @Override
    public void onPause() {
        super.onPause();
        placeViewModel.deletePlaces();
    }

    private List<String> getInterests() {
        fAuth = FirebaseAuth.getInstance();
        String userId = fAuth.getCurrentUser().getUid();
        SharedPreferences sharedPreferences = getActivity().getSharedPreferences(userId, getContext().MODE_PRIVATE);
        Set<String> interestSet = sharedPreferences.getStringSet(Constants.ALL_USER_INTERESTS, null);
        List<String> allInterests = new ArrayList<>();
        Log.d(TAG, "getInterests: " + interestSet);
        for (String interest : interestSet) {
            allInterests.add("+" + interest);

        }
        Log.d(TAG, "getInterests: " +  allInterests);
        return allInterests;
    }

    private List<Place> getPlacesList(ArrayList<String> query) {
        String linguaggio = "";

        if (Locale.getDefault().getLanguage().equals("it")) {
            linguaggio = "it";
        }
        else if (Locale.getDefault().getLanguage().equals("en")) {
            linguaggio = "en";
        }

        Resource<List<Place>> placeListResource = placeViewModel.getPlaces(query, linguaggio).getValue();

        if (placeListResource != null) {
            return placeListResource.getData();
        }

        return null;
    }
}
