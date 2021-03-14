package it.unimib.turistafelice.repository;

import android.util.Log;

import androidx.lifecycle.MutableLiveData;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

import it.unimib.turistafelice.model.Place;
import it.unimib.turistafelice.model.PlaceApiResponse;
import it.unimib.turistafelice.model.PlaceIdApiResponse;
import it.unimib.turistafelice.model.Resource;
import it.unimib.turistafelice.service.PlaceService;
import it.unimib.turistafelice.utils.Constants;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public class PlaceRepository {
    private static final String TAG = "PlaceRepository";
    private static PlaceRepository instance;
    private PlaceService placeService;


    private PlaceRepository() {
        Retrofit retrofit = new Retrofit.Builder()                  //retrofit mi permette di fare la richiesta HTTP
                .baseUrl(Constants.PLACE_BASE_API_URL)
                .addConverterFactory(GsonConverterFactory.create())   //per fare il parsing del oggetto json in automatico
                .build();

        placeService = retrofit.create(PlaceService.class);
    }

    public static synchronized PlaceRepository getInstance() {
        if (instance == null) {
            instance = new PlaceRepository();
        }

        return instance;
    }

    public void getFavoritePlaces(MutableLiveData<Resource<List<Place>>> placesResource, List<String> queries, String language) {
        Log.d(TAG, "Cercherò url per questi place Id " + queries);
        for (String query : queries) {
            Call<PlaceIdApiResponse> placeApiResponseCall = placeService.getFavoritesPlaces(query, language, Constants.API_KEY);
            Log.d(TAG, "getPlaces: " + placeService.getFavoritesPlaces(query, language, Constants.API_KEY).request().url());

            placeApiResponseCall.enqueue(new Callback<PlaceIdApiResponse>() {
                @Override
                public void onResponse(Call<PlaceIdApiResponse> call, Response<PlaceIdApiResponse> response) {
                    Log.d(TAG, "Chiamata andata bene? " + response.isSuccessful());
                    Log.d(TAG, "contenuto risposta: " + response.body());
                    if (response.isSuccessful() && response.body() != null) {
                        Resource<List<Place>> resource = new Resource<>();
                        if (placesResource.getValue() != null && placesResource.getValue().getData() != null) {
                            List<Place> currentUrls = placesResource.getValue().getData();
                            currentUrls.add(response.body().getResult());
                            resource.setData(currentUrls);
                        } else {
                            List<Place> firstPlace = new ArrayList<>();
                            firstPlace.add(response.body().getResult());
                            resource.setData(firstPlace);
                        }
                        Log.d(TAG, "array " + resource.getData());
                        placesResource.setValue(resource);
                        Log.d(TAG, "onResponse: " + placesResource.getValue().getData());
                    } else if (response.errorBody() != null) {
                        Resource<List<Place>> resource = new Resource<>();
                        try {
                            Log.d(TAG, "onResponse: " + response.errorBody().string() + "- " + response.message());
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                        placesResource.postValue(resource);
                    }
                }

                @Override
                public void onFailure(Call<PlaceIdApiResponse> call, Throwable t) {
                    Log.d(TAG, "onFailure: " + t.getMessage());
                }
            });
        }
    }

    public void getPlaces(MutableLiveData<Resource<List<Place>>> placesResource, List<String> queries, String language) {
        Log.d(TAG, "Cercherò url per questi place Id " + queries);
        List<Place> places = new ArrayList<Place>() ;
        for (String query : queries) {
            Call<PlaceApiResponse> placeApiResponseCall = placeService.getPlaces(query, language, Constants.API_KEY);
            Log.d(TAG, "getPlaces: " + placeService.getPlaces(query, language, Constants.API_KEY).request().url());

            placeApiResponseCall.enqueue(new Callback<PlaceApiResponse>() {
                @Override
                public void onResponse(Call<PlaceApiResponse> call, Response<PlaceApiResponse> response) {
                    Log.d(TAG, "Chiamata andata bene? " + response.isSuccessful());
                    Log.d(TAG, "contenuto risposta: " + response.body());
                    if (response.isSuccessful() && response.body() != null) {
                        Resource<List<Place>> resource = new Resource<>();
                        places.addAll(response.body().getResults());
                        resource.setData(places);
                        Log.d(TAG, "array " + resource.getData());
                        placesResource.setValue(resource);
                        Log.d(TAG, "onResponse: " + placesResource.getValue().getData());
                    } else if (response.errorBody() != null) {
                        Resource<List<Place>> resource = new Resource<>();
                        try {
                            Log.d(TAG, "onResponse: " + response.errorBody().string() + "- " + response.message());
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                        placesResource.postValue(resource);
                    }
                }

                @Override
                public void onFailure(Call<PlaceApiResponse> call, Throwable t) {
                    Log.d(TAG, "onFailure: " + t.getMessage());
                }
            });
        }
    }
}