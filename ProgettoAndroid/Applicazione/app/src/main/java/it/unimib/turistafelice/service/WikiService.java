package it.unimib.turistafelice.service;

import it.unimib.turistafelice.model.WikiTextApiResponse;
import it.unimib.turistafelice.model.WikiTitleApiResponse;
import retrofit2.Call;
import retrofit2.http.GET;
import retrofit2.http.Query;

public interface WikiService {

    @GET("w/api.php")
    Call<WikiTitleApiResponse> getTitle(@Query("action") String action,
                                        @Query("format") String format,
                                        @Query("list") String list,
                                        @Query("srsearch") String srsearch);


    @GET("w/api.php")
    Call<WikiTextApiResponse> getExtractedText(@Query("action") String action,
                                               @Query("format") String format,
                                               @Query("prop") String prop,
                                               @Query("exsentences") int exsentences,
                                               @Query("exlimit") int exlimit,
                                               @Query("explaintext") int explaintext,
                                               @Query("formatversion") int formatversion,
                                               @Query("titles") String titles);

}
