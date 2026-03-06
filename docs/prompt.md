List of features
- Upload a file or photo, if on mobile take a picture or from gallery
- Admin set tax rate
- doc

v─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
❯ here is a list of features I'd like to add, List of features
  - Upload a file or photo, if on mobile take a picture or from gallery                                                                                  
  - Admin set tax rate, in addition to these i want you to compile a list of new features that should be added to this app both on the customer facing   
  one and also the admin. Prioritize mobile friendliness and user experience. Create a new md doc that has this list and rank them in terms of           
  importance. Please also interact with the app through the web broswer     


Please read the ux-evaluation.md. There is a list of bugs there, take each bug in order and only work on one at a time. Write a failing test first for the bug, then introduce your fix for the bug. Re run your failed test, with your fix introduced it should pass the test. Afterwards ask yourself if there is somewhere related to this item in the codebase that you should fix as well? If there is prompt your user with a description of what you see as related. If not, now that you've solved the bug, was there a more elegant way you could have solved it? If so then do that in a way that your tests still pass. At the end, make sure to build the project and run all tests passing.



> Read `/home/keeps/Dev/MannahHp/MannaHp/docs/ux-evaluation.md`. It contains a list of bugs — work through them in order, one at a time, using this process for each:
>
> 1. Write a failing test that reproduces the bug.
> 2. Implement a fix, then re-run the test to confirm it passes.
> 3. Check the codebase for any other places where the same logic or behavior occurs. If you find any, describe what you see to the user and discuss whether those areas need to be updated for consistency before continuing.
> 4. Consider whether a more elegant solution exists. If so, refactor accordingly and verify your tests still pass.
> 5. Build the project and run all tests to confirm nothing else has broken before moving on to the next bug.
> 6. Append to ux-evaluation.md on that bug and update its status.
> 7. Append any pertientn info to the bug entry as to how you fixed it for future developers.
> Repeat this process for each bug in the list.





> Update the `/home/keeps/Dev/MannahHp/MannaHp/docs/ux-evaluation.md` with each of these items. Pleaseformat them like this 
#### Cart state not persisted across page reloads (was Critical #1)
- **Status:** BROKEN
- **Verified:** Added a Latte ($4.75) to cart, reloaded page — cart badge still showed $4.75 after reload. Cart now persists via localStorage.

- burrito bowl added to card toast should show above the Bowl Total so it does not block when the user clicks the card to pay and then the payment options are at the bottom as well.
- After paying for order the Order number blurb needs to display at the top fourth fo the screen.
- ON the buown builder screen, when the user clicks the ingredient card once it adds the quanity 1 and then when the click again it subtracts it. Instead the user should be able to click anywhere on the card and increase the quanitity except for a minus indicator which will then deincrement the quanity.
- If a user has just added a bowl to the cart, goes to the cart menu then clicks edit, the burrito bowl screen is not populated, however if the user is anywhere else the edit bowl will allow them to go back to the build bowl screen and see it populated with their previous choices.


 It contains a list of bugs — work through them in order, one at a time, using this process for each:
>
> 1. Write a failing test that reproduces the bug.
> 2. Implement a fix, then re-run the test to confirm it passes.
> 3. Check the codebase for any other places where the same logic or behavior occurs. If you find any, describe what you see to the user and discuss whether those areas need to be updated for consistency before continuing.
> 4. Consider whether a more elegant solution exists. If so, refactor accordingly and verify your tests still pass.
> 5. Build the project and run all tests to confirm nothing else has broken before moving on to the next bug.
> 6. Append to ux-evaluation.md on that bug and update its status.
> 7. Append any pertientn info to the bug entry as to how you fixed it for future developers.
> Repeat this process for each bug in the list.


- burrito bowl added to card toast should show above the Bowl Total so it does not block when the user clicks the card to pay and then the payment options are at the bottom as well.
- After paying for order the Order number blurb needs to display at the top fourth fo the screen.
- ON the buown builder screen, when the user clicks the ingredient card once it adds the quanity 1 and then when the click again it subtracts it. Instead the user should be able to click anywhere on the card and increase the quanitity except for a minus indicator which will then deincrement the quanity.
- If a user has just added a bowl to the cart, goes to the cart menu then clicks edit, the burrito bowl screen is not populated, however if the user is anywhere else the edit bowl will allow them to go back to the build bowl screen and see it populated with their previous choices.
