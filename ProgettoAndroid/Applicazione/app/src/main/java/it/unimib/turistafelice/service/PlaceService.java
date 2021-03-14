package it.unimib.turistafelice.service;

import it.unimib.turistafelice.model.PlaceApiResponse;
import it.unimib.turistafelice.model.PlaceIdApiResponse;
import retrofit2.Call;
import retrofit2.http.GET;
import retrofit2.http.Query;

public interface PlaceService {

    @GET("textsearch/json")
    Call<PlaceApiResponse> getPlaces(@Query("query") String query,
                                     @Query("language") String language,
                                     @Query("key") String key);

    @GET("details/json")
    Call<PlaceIdApiResponse> getFavoritesPlaces(@Query("placeid") String query,
                                                @Query("language") String language,
                                                @Query("key") String key);

}

