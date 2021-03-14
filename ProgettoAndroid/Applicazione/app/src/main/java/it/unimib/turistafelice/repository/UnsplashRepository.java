package it.unimib.turistafelice.repository;

import android.util.Log;

import androidx.lifecycle.MutableLiveData;

import java.io.IOException;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import it.unimib.turistafelice.model.Resource;
import it.unimib.turistafelice.model.UnsplashApiResponse;
import it.unimib.turistafelice.service.UnsplashService;
import it.unimib.turistafelice.utils.Constants;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public class UnsplashRepository {
    private static final String TAG = "UnsplashRepository";
    private static UnsplashRepository instance;
    private UnsplashService unsplashService;

    private UnsplashRepository() {
        Retrofit retrofit = new Retrofit.Builder()
                .baseUrl(Constants.UNSPLASH_PHOTO_BASE_API_URL)
                .addConverterFactory(GsonConverterFactory.create())
                .build();
        unsplashService = retrofit.create(UnsplashService.class);
    }

    public static synchronized UnsplashRepository getInstance() {
        if (instance == null) {
            instance = new UnsplashRepository();
        }

        return instance;
    }

    public void getPhotoUrl(MutableLiveData<Resource<Map<String, String>>> photoUrlList, List<String> cityNames) {
        Log.d(TAG, "Cercherò url per queste città "+cityNames);
        for (String cityName: cityNames) {
            Call<UnsplashApiResponse> unsplashApiCall = unsplashService.getPhotoUrl(cityName+Constants.UNSPLASH_CATEGORY, Constants.UNSPLASH_PAGE,
                    Constants.UNSPLASH_PER_PAGE, Constants.UNSPLASH_PHOTO_KEY);
            Log.d(TAG, "getPhotoUrl: " + unsplashService.getPhotoUrl(cityName+Constants.UNSPLASH_CATEGORY, Constants.UNSPLASH_PAGE,
                    Constants.UNSPLASH_PER_PAGE, Constants.UNSPLASH_PHOTO_KEY).request().url());
            unsplashApiCall.enqueue(new Callback<UnsplashApiResponse>() {
                @Override
                public void onResponse(Call<UnsplashApiResponse> call, Response<UnsplashApiResponse> response) {
                    Log.d(TAG, "Chiamata andata bene? "+ response.isSuccessful());
                    Log.d(TAG, "contenuto risposta: "+ response.body());
                    if (response.isSuccessful() && response.body() != null) {
                        Resource<Map<String, String>> resource = new Resource<>();
                        if(photoUrlList.getValue() != null && photoUrlList.getValue().getData() != null){
                            Map<String, String> currentUrls = photoUrlList.getValue().getData();
                            if(response.body().getResults().size() != 0){
                                currentUrls.put(cityName, response.body().getResults().get(0).getUrls().getRegular());
                            } else {
                                currentUrls.put(cityName, Constants.NO_IMAGE_FOUND);
                            }

                            resource.setData(currentUrls);
                        }
                        else{
                            Map<String, String> firstUrl = new HashMap<>();
                            if(response.body().getResults().size() != 0){
                                firstUrl.put(cityName, response.body().getResults().get(0).getUrls().getRegular());
                            } else {
                                firstUrl.put(cityName, Constants.NO_IMAGE_FOUND);
                            }
                            resource.setData(firstUrl);
                        }
                        Log.d(TAG, "array "+ resource.getData());
                        photoUrlList.setValue(resource);
                        Log.d(TAG, "onResponse: "+photoUrlList.getValue().getData());
                    }else if (response.errorBody() != null) {
                        Resource<Map<String, String>> resource = new Resource<>();
                        try {
                            Log.d(TAG, "onResponse: "+response.errorBody().string() + "- " + response.message());
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                        photoUrlList.postValue(resource);
                    }
                }

                @Override
                public void onFailure(Call<UnsplashApiResponse> call, Throwable t) {
                    Log.d(TAG, "onFailure: "+t.getMessage());
                }
            });

        }
    }



}
