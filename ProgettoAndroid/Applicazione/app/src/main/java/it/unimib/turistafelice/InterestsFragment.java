package it.unimib.turistafelice;

import android.content.Context;
import android.content.SharedPreferences;
import android.os.Bundle;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.CheckBox;
import android.widget.ExpandableListAdapter;
import android.widget.ExpandableListView;

import com.google.android.material.snackbar.Snackbar;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.firestore.DocumentReference;
import com.google.firebase.firestore.FirebaseFirestore;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;

import it.unimib.turistafelice.adapter.InterestsExpListAdapter;
import it.unimib.turistafelice.utils.Constants;
import it.unimib.turistafelice.databinding.FragmentInterestsBinding;
import it.unimib.turistafelice.databinding.InterestsFooterBinding;
import it.unimib.turistafelice.databinding.InterestsHeaderBinding;



public class InterestsFragment extends Fragment {

    private FragmentInterestsBinding binding;
    private InterestsFooterBinding bindingFooter;
    private InterestsHeaderBinding bindingHeader;
    private FirebaseAuth fAuth;
    private FirebaseFirestore fStore;
    private String userId;
    private ExpandableListAdapter listAdapter;

    private Set<String> userInterestsSet;
    private Map<String, List<String>> interestsPerCategory;
    private List<String> categories;

    public InterestsFragment() { }


    // TODO: Rename and change types and number of parameters
    public static InterestsFragment newInstance(String param1, String param2) {
        InterestsFragment fragment = new InterestsFragment();
        return fragment;
    }
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        binding = binding.inflate(getLayoutInflater());
        return binding.getRoot();
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        fAuth = FirebaseAuth.getInstance();
        fStore = FirebaseFirestore.getInstance();
        userId = fAuth.getCurrentUser().getUid();

        interestsPerCategory = new HashMap<>();
        categories = new ArrayList<>();
        userInterestsSet = new HashSet<>();

        Context context = getActivity();
        SharedPreferences sharedPreferences = context.getSharedPreferences(userId, getContext().MODE_PRIVATE);
        SharedPreferences.Editor editor = sharedPreferences.edit();
        userInterestsSet = sharedPreferences.getStringSet(Constants.ALL_USER_INTERESTS, null);

        fillData();

        bindingFooter = bindingFooter.inflate(getLayoutInflater());
        bindingHeader = bindingHeader.inflate(getLayoutInflater());
        bindingFooter.saveChangeInterestsButton.setVisibility(View.GONE);


        listAdapter = new InterestsExpListAdapter(this.getContext(), interestsPerCategory, categories , userId);

        //creiamo la vista espandibile ma abbiamo bisogno di un adapter, che pero è una classe astratta
        binding.interestsExpandableListView.addHeaderView(bindingHeader.getRoot());
        binding.interestsExpandableListView.addFooterView(bindingFooter.getRoot());
        binding.interestsExpandableListView.setAdapter(listAdapter);
        binding.interestsExpandableListView.setClickable(false);

        binding.interestsExpandableListView.setChildDivider(getResources().getDrawable(R.color.quantum_white_100));
        binding.interestsExpandableListView.setGroupIndicator(getResources().getDrawable(R.drawable.ic_arrow_down));


        binding.interestsExpandableListView.setOnChildClickListener(new ExpandableListView.OnChildClickListener() {
            @Override
            public boolean onChildClick(ExpandableListView parent, View v, int groupPosition, int childPosition, long id) {
                Snackbar.make(v, getResources().getString(R.string.changes_not_allowed), Snackbar.LENGTH_SHORT).show();
                return false;
            }
        });



        bindingFooter.changeInterestsButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                bindingHeader.yourInterestsMainTextView.setText(R.string.change_your_interests);
                //bindingHeader.yourInterestsTipsTextView.setText(R.string.change_your_interests_tips);

                bindingFooter.changeInterestsButton.setVisibility(View.GONE);
                bindingFooter.saveChangeInterestsButton.setVisibility(View.VISIBLE);

                binding.interestsExpandableListView.setClickable(true);


                binding.interestsExpandableListView.setOnChildClickListener(new ExpandableListView.OnChildClickListener() {
                    @Override
                    public boolean onChildClick(ExpandableListView parent, View v, int groupPosition, int childPosition, long id) {

                        final String interesse = (String) listAdapter.getChild(groupPosition, childPosition);
                        CheckBox interestCheckBox = v.findViewById(R.id.isInterestWantedCheckbox);

                        if(userInterestsSet.contains(interesse)){
                            userInterestsSet.remove(interesse);
                            interestCheckBox.setChecked(false);
                        }else{
                            userInterestsSet.add(interesse);
                            interestCheckBox.setChecked(true);
                        }
                        editor.remove(Constants.ALL_USER_INTERESTS);
                        editor.apply();

                        editor.putStringSet(Constants.ALL_USER_INTERESTS, userInterestsSet);
                        editor.apply();

                        return false;
                    }

                });
            }
        });

        bindingFooter.saveChangeInterestsButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                binding.interestsExpandableListView.setClickable(false);
                binding.interestsExpandableListView.setOnChildClickListener(new ExpandableListView.OnChildClickListener() {
                    @Override
                    public boolean onChildClick(ExpandableListView parent, View v, int groupPosition, int childPosition, long id) {
                        Snackbar.make(v, getResources().getString(R.string.changes_not_allowed), Snackbar.LENGTH_SHORT).show();
                        return false;
                    }
                });

                bindingFooter.saveChangeInterestsButton.setVisibility(View.GONE);
                bindingFooter.changeInterestsButton.setVisibility(View.VISIBLE);

                bindingHeader.yourInterestsMainTextView.setText(R.string.your_interests);

                DocumentReference documentReference = fStore.collection(Constants.USER)
                        .document(userId).collection(Constants.ALL_USER_INTERESTS).document(Constants.ALL_USER_INTERESTS);

                Map<String, Object> userInterestsDoc = new HashMap<>();
                ArrayList<String> userInterests = new ArrayList<>();
                userInterests.addAll(userInterestsSet);
                userInterestsDoc.put(Constants.ALL_USER_INTERESTS, userInterests);
                documentReference.update(userInterestsDoc);
            }
        });
    }



    private void fillData(){

        //carico stringhe diverse in base alla lingua

        if (Locale.getDefault().getLanguage().equals("it")) {

            List<String> arte = new ArrayList<>(Arrays.asList(Constants.ARTE));
            List<String> cibo = new ArrayList<>(Arrays.asList(Constants.CIBO));
            List<String> relax = new ArrayList<>(Arrays.asList(Constants.RELAX_IT));
            List<String> sport = new ArrayList<>(Arrays.asList(Constants.SPORT_IT));
            List<String> salute = new ArrayList<>(Arrays.asList(Constants.SALUTE));

            interestsPerCategory.put(Constants.CATEGORIE[0], arte);
            interestsPerCategory.put(Constants.CATEGORIE[1], cibo);
            interestsPerCategory.put(Constants.CATEGORIE[2], relax);
            interestsPerCategory.put(Constants.CATEGORIE[3], sport);
            interestsPerCategory.put(Constants.CATEGORIE[4], salute);


            categories.addAll(Arrays.asList(Constants.CATEGORIE));
        }
        else if (Locale.getDefault().getLanguage().equals("en")) {

            List<String> art = new ArrayList<>(Arrays.asList(Constants.ART));
            List<String> food = new ArrayList<>(Arrays.asList(Constants.FOOD));
            List<String> relax = new ArrayList<>(Arrays.asList(Constants.RELAX_EN));
            List<String> sport = new ArrayList<>(Arrays.asList(Constants.SPORT_EN));
            List<String> health = new ArrayList<>(Arrays.asList(Constants.HEALTH));

            interestsPerCategory.put(Constants.CATEGORIES[0], art);
            interestsPerCategory.put(Constants.CATEGORIES[1], food);
            interestsPerCategory.put(Constants.CATEGORIES[2], relax);
            interestsPerCategory.put(Constants.CATEGORIES[3], sport);
            interestsPerCategory.put(Constants.CATEGORIE[4], health);


            categories.addAll(Arrays.asList(Constants.CATEGORIES));
        }




    }
}


