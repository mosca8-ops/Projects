package it.unimib.turistafelice.viewmodel;

import android.util.Log;

import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;
import androidx.lifecycle.ViewModel;

import java.util.List;

import it.unimib.turistafelice.model.Place;
import it.unimib.turistafelice.model.Resource;
import it.unimib.turistafelice.repository.PlaceRepository;

public class PlaceViewModel extends ViewModel {
    private static final String TAG="PlaceViewModel";
    private MutableLiveData<Resource<List<Place>>> places;
    private MutableLiveData<Resource<List<Place>>> favoritePlaces;

    public LiveData<Resource<List<Place>>> getPlaces(List<String> query, String language) {
        Log.d(TAG, "getPlaces: "+places);
        if (places == null) {
            places = new MutableLiveData<>();
            Log.d(TAG, "getPlaces: Download places from Google MAPS API");
            PlaceRepository.getInstance().getPlaces(places, query, language);
        }
        return places;
    }

    public LiveData<Resource<List<Place>>> getFavoritePlaces(List<String> query, String language) {
        favoritePlaces = new MutableLiveData<>();
        Log.d(TAG, "getPlaces: Download places from Google MAPS API");
        PlaceRepository.getInstance().getFavoritePlaces(favoritePlaces, query, language);
        Log.d(TAG, "getPlaces: "+favoritePlaces);

        return favoritePlaces;
    }

    public void deletePlaces() {
        places = null;
    }
}

