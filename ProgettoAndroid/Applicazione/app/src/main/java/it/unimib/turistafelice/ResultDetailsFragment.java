package it.unimib.turistafelice;

import android.content.Context;
import android.content.SharedPreferences;
import android.os.Bundle;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.lifecycle.Observer;
import androidx.lifecycle.ViewModelProvider;

import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import com.google.android.material.snackbar.Snackbar;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.firestore.DocumentReference;
import com.google.firebase.firestore.FirebaseFirestore;
import com.squareup.picasso.Picasso;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import it.unimib.turistafelice.databinding.FragmentResultDetailsBinding;
import it.unimib.turistafelice.model.Place;
import it.unimib.turistafelice.model.WikiTextApiResponse;
import it.unimib.turistafelice.utils.Constants;
import it.unimib.turistafelice.viewmodel.WikiTextViewModel;


public class ResultDetailsFragment extends Fragment {

    private static final String TAG = "ResultDetailsFragment";
    private FragmentResultDetailsBinding binding;
    private WikiTextViewModel wikiTextViewModel;
    private Set<String> userTripsSet, userFavoritesSet;
    private String userId, trip;
    private FirebaseAuth fAuth;
    private FirebaseFirestore fStore;
    private List<String> trips;
    private Map<String, Object> userFavoritesDoc;
    private Map<String, Object> userTripsDoc;
    private DocumentReference documentReferenceInterests;
    private DocumentReference documentReference;

    public ResultDetailsFragment() {
        // Required empty public constructor
    }


    public static ResultDetailsFragment newInstance() {
        ResultDetailsFragment fragment = new ResultDetailsFragment();
        Bundle args = new Bundle();
        fragment.setArguments(args);
        return fragment;
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        // Inflate the layout for this fragment
        binding = FragmentResultDetailsBinding.inflate(getLayoutInflater());
        return binding.getRoot();
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        userTripsSet = new HashSet<>();
        userTripsDoc = new HashMap<>();
        userFavoritesSet = new HashSet<>();
        fAuth = FirebaseAuth.getInstance();
        fStore = FirebaseFirestore.getInstance();
        userId = fAuth.getCurrentUser().getUid();
        userFavoritesDoc = new HashMap<>();

        trip = (String) getArguments().get("city_name").toString().toUpperCase();


        documentReference = fStore.collection(Constants.USER)
                .document(userId).collection(Constants.ALL_TRIPS).document(Constants.ALL_TRIPS);

        Context context = getActivity();
        SharedPreferences sharedPreferences = context.getSharedPreferences(userId, getContext().MODE_PRIVATE);
        SharedPreferences.Editor editor = sharedPreferences.edit();

        userTripsSet = sharedPreferences.getStringSet(Constants.ALL_TRIPS, null);

        wikiTextViewModel = new ViewModelProvider(requireActivity()).get(WikiTextViewModel.class);

        Place placeSelected = (Place) getArguments().get("placeDetailArg");
        String placePhotoUrl = getUrlPath(placeSelected);
        String placeID = placeSelected.getPlace_id();


        Picasso.get().load(placePhotoUrl).fit().into(binding.imageViewWikipedia);

        wikiTextViewModel.getText(placeSelected.getName()).observe(getViewLifecycleOwner(), new Observer<WikiTextApiResponse>() {
            @Override
            public void onChanged(WikiTextApiResponse wikiTextApiResponse) {
                binding.textViewTitlePlace.setText(wikiTextApiResponse.getQuery().getPages().get(0).getTitle());
                binding.textViewWikipedia.setText(wikiTextApiResponse.getQuery().getPages().get(0).getExtract());
            }
        });


        if (userTripsSet.contains(trip) ) {         //controllo se il cuoricino deve essere checked o meno
            userFavoritesSet = sharedPreferences.getStringSet(trip, null);
            Log.d(TAG, "onViewCreated: " + userFavoritesSet);
            if(userFavoritesSet.contains(placeID)){
                binding.iconFavorite.setChecked(true);
            }
        }


        binding.iconFavorite.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                userTripsSet = sharedPreferences.getStringSet(Constants.ALL_TRIPS, null);
                if (binding.iconFavorite.isChecked()) {
                    if (!userTripsSet.contains(trip)) { //aggiungo place id nel caso in cui non c'è ancora la città

                        ArrayList<String> userTrips = new ArrayList<>();
                        userTripsSet.add(trip);
                        userTrips.addAll(userTripsSet);
                        userTripsDoc.put(Constants.ALL_TRIPS, userTrips);
                        documentReference.update(userTripsDoc);
                        editor.remove(Constants.ALL_TRIPS);
                        editor.putStringSet(Constants.ALL_TRIPS, userTripsSet);
                        editor.apply();
                    }else{
                        Log.d(TAG, "onClick: " + sharedPreferences.getStringSet(trip, null));
                        userFavoritesSet = sharedPreferences.getStringSet(trip, null);
                    }
                    userFavoritesSet.add(placeID);
                    ArrayList<String> userFavorites = new ArrayList<>();
                    userFavorites.addAll(userFavoritesSet);
                    userFavoritesDoc.put(trip, userFavorites);
                    documentReferenceInterests = fStore.collection(Constants.USER)
                            .document(userId).collection(trip).document(trip);
                    documentReferenceInterests.set(userFavoritesDoc);
                    Log.d(TAG, "onClick: " + trip + "  " + userFavoritesSet);
                    editor.remove(trip);
                    editor.apply();

                    editor.putStringSet(trip, userFavoritesSet);
                    editor.apply();
                    Snackbar.make(v, R.string.place_added, Snackbar.LENGTH_SHORT).show();
                }else{
                    userFavoritesSet = sharedPreferences.getStringSet(trip, null);
                    userFavoritesSet.remove(placeID);
                    ArrayList<String> allTripPlaceList = new ArrayList<>();
                    allTripPlaceList.addAll(userFavoritesSet);
                    userFavoritesDoc.put(trip, allTripPlaceList);

                    documentReferenceInterests = fStore.collection(Constants.USER)
                            .document(userId).collection(trip).document(trip);
                    documentReferenceInterests.update(userFavoritesDoc);

                    editor.remove(trip);
                    editor.apply();
                    editor.putStringSet(trip, userFavoritesSet);
                    editor.apply();
                    if(userFavoritesSet.isEmpty()){
                        Log.d(TAG, ""+ userTripsSet);
                        Log.d(TAG, trip);
                        userTripsSet.remove(trip);

                        ArrayList<String> allTripList = new ArrayList<>();
                        allTripList.addAll(userTripsSet);
                        userTripsDoc.put(Constants.ALL_TRIPS, allTripList);
                        documentReference.update(userTripsDoc);
                        editor.remove(trip);
                        editor.apply();
                        documentReferenceInterests.delete();
                    }
                    Snackbar.make(v, R.string.place_removed, Snackbar.LENGTH_SHORT).show();
                }
            }
        });
    }

    private String getUrlPath(Place placeSelected) {
        if(placeSelected.getPhotos() != null){
            String Path = Constants.PLACE_PHOTO_API_URL + "photoreference="
                    + placeSelected.getPhotos().get(0).getPhoto_reference()
                    + "&maxwidth=" + placeSelected.getPhotos().get(0).getWidth()
                    + "&key=" + Constants.API_KEY;
            Log.d(TAG, "getPath: " + Path);
            return Path;
        } else {
            return Constants.NO_IMAGE_FOUND;
        }

    }
}
