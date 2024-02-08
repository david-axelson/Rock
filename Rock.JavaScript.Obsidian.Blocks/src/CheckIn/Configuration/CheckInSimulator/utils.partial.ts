import { RockDateTime } from "@Obsidian/Utility/rockDateTime";
import { Ref, reactive, ref } from "vue";

export class CheckInStep {
    /** The current state of the step. */
    public readonly state: Ref<"Running" | "OK" | "Error" | "NotRun">;

    /** An error message to display if state was Error. */
    public readonly errorMessage: Ref<string | undefined>;

    /** The last time this step was executed. */
    public readonly lastExecution: Ref<string | undefined>;

    /** The duration (or average duration) in milliseconds of each step. */
    public readonly executionDurations: number[];

    /** The targer number of executions when running in bulk. */
    public readonly targetExecutionCount: Ref<number | undefined>;

    /** The funciton that will execute the step. */
    private executor: () => Promise<void>;

    /** The previous step to use when chaining steps. */
    private previousStep?: CheckInStep;

    /** True if the current execution is inside a batch. */
    private isBatchExecution: boolean;

    /**
     * Creates a new step to be executed.
     *
     * @param executor The function to call to execute the step logic.
     * @param previousStep The previous step when chaining is enabled.
     */
    public constructor(executor: () => Promise<void>, previousStep?: CheckInStep) {
        this.state = ref("NotRun");
        this.errorMessage = ref(undefined);
        this.lastExecution = ref(undefined);
        this.executionDurations = reactive([]);
        this.targetExecutionCount = ref(undefined);
        this.executor = executor;
        this.previousStep = previousStep;
        this.isBatchExecution = false;
    }

    /**
     * Starts a new batch for multiple executes.
     *
     * @param iterationCount The number of iterations in the batch.
     * @param executePreviousSteps True if the previous step will be executed as part of the batch.
     */
    private startBatch(iterationCount: number | undefined, executePreviousSteps: boolean): void {
        this.isBatchExecution = true;
        this.state.value = "Running";
        this.errorMessage.value = undefined;
        this.targetExecutionCount.value = iterationCount;
        this.executionDurations.splice(0, this.executionDurations.length);
        this.lastExecution.value = RockDateTime.now().toASPString("G");

        if (executePreviousSteps) {
            this.previousStep?.startBatch(iterationCount, executePreviousSteps);
        }
    }

    /**
     * Stops a current batch after all executions have completed.
     */
    private stopBatch(): void {
        this.previousStep?.stopBatch();
        this.state.value = !this.errorMessage.value ? "OK" : "Error";
        this.isBatchExecution = false;
    }

    /**
     * Executes this step a single time.
     *
     * @param executePreviousSteps True if the previous steps should be executed again.
     */
    public async execute(executePreviousSteps: boolean): Promise<void> {
        if (executePreviousSteps) {
            try {
                this.previousStep?.execute(executePreviousSteps);
            }
            catch {
                this.state.value = "Error";
                this.errorMessage.value = "Previous step failed.";

                return;
            }
        }

        if (!this.isBatchExecution) {
            this.state.value = "Running";
            this.errorMessage.value = undefined;
            this.targetExecutionCount.value = undefined;
            this.executionDurations.splice(0, this.executionDurations.length);
            this.lastExecution.value = RockDateTime.now().toASPString("G");
        }

        const startTime = performance.now();

        try {
            await this.executor();

            if (!this.isBatchExecution) {
                this.state.value = "OK";
                this.errorMessage.value = undefined;
            }
        }
        catch (error) {
            this.state.value = "Error";
            this.errorMessage.value = error instanceof Error ? error.message : `${error}`;

            throw error;
        }
        finally {
            this.executionDurations.push(performance.now() - startTime);
        }
    }

    /**
     * Executes this step a number of times in a batch.
     *
     * @param executePreviousSteps True if the previous steps should be executed again.
     */
    public executeBatch(iterationCount: number | undefined, maximumDuration: number | undefined, executePreviousSteps: boolean): Promise<void> {
        return new Promise<void>(resolve => {
            if (!iterationCount && !maximumDuration) {
                return resolve();
            }

            const startTime = performance.now();
            this.startBatch(iterationCount, executePreviousSteps);

            const doStep = async (): Promise<void> => {
                try {
                    await this.execute(executePreviousSteps);
                }
                catch (error) {
                    this.stopBatch();

                    return resolve();
                }

                if (iterationCount && this.executionDurations.length >= iterationCount) {
                    this.errorMessage.value = undefined;
                    this.stopBatch();

                    return resolve();
                }

                if (maximumDuration && (performance.now() - startTime) >= (maximumDuration * 1000)) {
                    this.errorMessage.value = undefined;
                    this.stopBatch();

                    return resolve();
                }

                setTimeout(doStep, 0);
            };

            doStep();
        });
    }
}
