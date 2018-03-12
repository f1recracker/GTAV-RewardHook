
# TODO
- Add configuration files to change parameters. Examples:
    - episode size in millis
	- default events-to-rewards scheme
	- auto start capture settings
	- add ability to show / hide notifications
- **[IMP]** Setup a RESTful server. Sample queries include:
	- `/episode/{nearest_epoch}`
	- `/reward/{nearest_epoch}` (using the default reward scheme)
	- `/trainer/{start|stop}`
	- `/trainer/configure`
- Add simple examples (in python) that demonstrate the process of generating RL datasets using RewardHook
- Add more events that can be used!
