import tensorflow as tf
import numpy as np

# load data
with open('C:/Users/sebas/source/repos/Chess/Chess/output.txt', 'r') as f:
    lines = f.readlines()

inputs = []
outputs = []
for line in lines:
    board, score = line.strip().split()
    inputs.append([int(x) for x in board])
    outputs.append(float(score))

# define the model
model = tf.keras.Sequential([
    tf.keras.layers.Dense(128, activation='relu', input_shape=(65,)),
    tf.keras.layers.Dense(64, activation='relu'),
    tf.keras.layers.Dense(1, activation='linear')
])

# compile the model
model.compile(optimizer='adam', loss='mse')

# convert inputs and outputs to numpy arrays
inputs = np.array(inputs)
outputs = np.array(outputs)

# train the model
model.fit(inputs, outputs, epochs=1000)

# save the model in the SavedModel format
tf.saved_model.save(model, 'saved_model')
