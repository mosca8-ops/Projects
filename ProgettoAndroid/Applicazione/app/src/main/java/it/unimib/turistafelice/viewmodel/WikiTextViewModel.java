package it.unimib.turistafelice.viewmodel;

import android.util.Log;

import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;
import androidx.lifecycle.ViewModel;

import it.unimib.turistafelice.model.WikiTextApiResponse;
import it.unimib.turistafelice.repository.WikiRepository;

public class WikiTextViewModel extends ViewModel {

    private static final String TAG="WikiTextViewModel";
    private MutableLiveData<WikiTextApiResponse> textResponse;

    public LiveData<WikiTextApiResponse> getText(String title) {
        textResponse = new MutableLiveData<>();
        Log.d(TAG, "getText download from Wikipedia");
        WikiRepository.getInstance().getText(textResponse, title);
        return textResponse;
    }

}
