package it.unimib.turistafelice;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.navigation.Navigation;

import com.google.firebase.auth.FirebaseAuth;

import it.unimib.turistafelice.utils.RefreshUtil;

public class StartFragment extends Fragment {

    private static final String TAG = "StartFragment";
    private FirebaseAuth fAuth;
    private boolean notLogged = false;

    public StartFragment() {
        // Required empty public constructor
    }


    public static StartFragment newInstance() {
        StartFragment fragment = new StartFragment();
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
        return inflater.inflate(R.layout.fragment_start, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        fAuth = FirebaseAuth.getInstance();

        if(fAuth.getCurrentUser() == null){
            StartFragmentDirections.NotLoggedAction notLoggedAction =StartFragmentDirections.notLoggedAction();
            Navigation.findNavController(view).navigate(notLoggedAction);
        }
        else {
            RefreshUtil.refreshSharedPreferences(fAuth, getActivity(), getContext());
            StartFragmentDirections.LoggedAction loggedAction = StartFragmentDirections.loggedAction();
            Navigation.findNavController(view).navigate(loggedAction);
        }
    }
}
