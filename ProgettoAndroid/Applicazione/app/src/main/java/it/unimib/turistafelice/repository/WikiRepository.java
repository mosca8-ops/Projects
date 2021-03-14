package it.unimib.turistafelice.repository;

import android.util.Log;

import androidx.lifecycle.MutableLiveData;

import java.util.Locale;

import it.unimib.turistafelice.model.WikiTextApiResponse;
import it.unimib.turistafelice.model.WikiTitleApiResponse;
import it.unimib.turistafelice.service.WikiService;
import it.unimib.turistafelice.utils.Constants;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public class WikiRepository {

    private static final String TAG = "WikiRepository";
    private static WikiRepository instance;
    private WikiService wikiService;

    private WikiRepository() {

        if (Locale.getDefault().getLanguage().equals("it")) {
            Retrofit retrofit = new Retrofit.Builder()                  //retrofit mi permette di fare la richiesta HTTP
                    .baseUrl(Constants.WIKI_BASE_API_URL)
                    .addConverterFactory(GsonConverterFactory.create())   //per fare il parsing del oggetto json in automatico
                    .build();

            wikiService = retrofit.create(WikiService.class);
        }
        else if (Locale.getDefault().getLanguage().equals("en")) {
            Retrofit retrofit = new Retrofit.Builder()                  //retrofit mi permette di fare la richiesta HTTP
                    .baseUrl(Constants.WIKI_BASE_API_URL_EN)
                    .addConverterFactory(GsonConverterFactory.create())   //per fare il parsing del oggetto json in automatico
                    .build();

            wikiService = retrofit.create(WikiService.class);
        }
    }

    public static synchronized WikiRepository getInstance() {
        if (instance == null) {
            instance = new WikiRepository();
        }

        return instance;
    }


    public void getText(MutableLiveData<WikiTextApiResponse> textResource, String srcsearch) {
        Call<WikiTitleApiResponse> wikiTitleApiResponseCall = wikiService.getTitle(Constants.WIKI_ACTION,Constants.WIKI_FORMAT,Constants.WIKI_LIST, srcsearch);

        Log.d(TAG, "getTitle: "+ wikiService.getTitle(Constants.WIKI_ACTION,Constants.WIKI_FORMAT,Constants.WIKI_LIST, srcsearch).request().url());
        wikiTitleApiResponseCall.enqueue(new Callback<WikiTitleApiResponse>() {
            @Override
            public void onResponse(Call<WikiTitleApiResponse> call, Response<WikiTitleApiResponse> responseTitle) {
                if (responseTitle.isSuccessful()
                        && responseTitle.body() != null
                        && responseTitle.body().getQuery() != null
                        && responseTitle.body().getQuery().getSearch().size() != 0) {
                    Log.d(TAG, "onResponse: "+ responseTitle.body().toString());
                    String titles = responseTitle.body().getQuery().getSearch().get(0).getTitle();

                    Call<WikiTextApiResponse> wikiTextApiResponseCall = wikiService.getExtractedText(Constants.WIKI_ACTION,
                            Constants.WIKI_FORMAT, Constants.WIKI_PROP,
                            Constants.WIKI_EXSENTENCES, Constants.WIKI_EXLIMIT,
                            Constants.WIKI_EXPLAINTEXT, Constants.WIKI_FORMATVERSION, titles);

                    Log.d(TAG, "getText: "+ wikiService.getExtractedText(Constants.WIKI_ACTION,
                            Constants.WIKI_FORMAT, Constants.WIKI_PROP,
                            Constants.WIKI_EXSENTENCES, Constants.WIKI_EXLIMIT,
                            Constants.WIKI_EXPLAINTEXT, Constants.WIKI_FORMATVERSION, titles).request().url());

                    wikiTextApiResponseCall.enqueue(new Callback<WikiTextApiResponse>() {
                        @Override
                        public void onResponse(Call<WikiTextApiResponse> call, Response<WikiTextApiResponse> response) {
                            if(response.isSuccessful() && response.body() != null) {
                                Log.d(TAG, "body: "+ response.body());
                                Log.d(TAG, "text: "+ response.body().getQuery().getPages().get(0).getExtract());
                                textResource.postValue(response.body());
                            }
                        }

                        @Override
                        public void onFailure(Call<WikiTextApiResponse> call, Throwable t) {
                            Log.d(TAG, "Errore nel getText: "+ t.getMessage());
                        }
                    });
                }

            }
            @Override
            public void onFailure(Call<WikiTitleApiResponse> call, Throwable t) {
                Log.d(TAG, "Errore nel getTitle: "+ t.getMessage());
            }

        });
    }
}
