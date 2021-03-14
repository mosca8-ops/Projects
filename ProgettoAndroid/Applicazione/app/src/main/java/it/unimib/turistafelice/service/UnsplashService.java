package it.unimib.turistafelice.service;

import it.unimib.turistafelice.model.UnsplashApiResponse;
import retrofit2.Call;
import retrofit2.http.GET;
import retrofit2.http.Query;

public interface UnsplashService {

    @GET("search/photos")
    Call<UnsplashApiResponse> getPhotoUrl(@Query("query") String query,
                                          @Query("page") String page,
                                          @Query("per_page") String per_page,
                                          @Query("client_id") String client_id);
}
